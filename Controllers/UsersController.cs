using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Request;
using ArzanGo.Models.Requests;
using ArzanGo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly IKyrgyzstanTimeService _timeService;

        public UsersController(AppDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory, IKyrgyzstanTimeService timeService)
        {
            _context = context;
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(config["SmsProNikitaKg:ApiUrl"] ?? "https://smspro.nikita.kg/api/");
            _timeService= timeService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.Include(u => u.Orders)
                                     .Include(u => u.Favorites)
                                     .Include(u => u.ShippingAddresses)
                                     .ToListAsync();
        }

        [HttpGet("couriers")]
        public async Task<ActionResult<IEnumerable<User>>> GetCouriers()
        {
            return await _context.Users
                .Where(u => u.Courier == true)
                .ToListAsync();
        }

        [HttpGet("not-orders-couriers")]
        public async Task<ActionResult<IEnumerable<User>>> GetActiveCouriers()
        {
            return await _context.Users
                .Where(u => u.Courier == true)
                .Where(u => u.IsOnline == true)
                .Where(u => !u.Orders.Any(o => o.Status != Status.IsReceivedByCourier && o.Status != Status.IsOnTheWay))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _context.Users.Include(u => u.Orders)
                                         .Include(u => u.Favorites)
                                         .Include(u => u.ShippingAddresses)
                                         .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            user.Password = user.PhoneNumber;
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email already exists");

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber))
                return BadRequest("Phone number already exists");

            user.UserId = Guid.NewGuid();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UserUpdateModel model)
        {
            if (id != model.UserId)
                return BadRequest("ID in URL and body do not match");

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
                return NotFound("User not found");

            // Проверка и обновление Email (если он изменён)
            if (model.Email != null && model.Email != existingUser.Email)
            {
                bool emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email && u.UserId != id);

                if (emailExists)
                    return BadRequest("Email is already taken by another user");

                existingUser.Email = model.Email;
            }

            // Проверка и обновление PhoneNumber (если он изменён)
            if (model.PhoneNumber != null && model.PhoneNumber != existingUser.PhoneNumber)
            {
                bool phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.UserId != id);

                if (phoneExists)
                    return BadRequest("Phone number is already taken by another user");

                existingUser.PhoneNumber = model.PhoneNumber;
            }

            // Обновляем остальные поля
            existingUser.FirstName = model.FirstName ?? existingUser.FirstName;
            existingUser.LastName = model.LastName ?? existingUser.LastName;
            existingUser.Admin = model.Admin ?? existingUser.Admin;
            existingUser.Courier = model.Courier ?? existingUser.Courier;
            existingUser.Password = model.Password ?? existingUser.Password;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(existingUser);
            }
            catch (DbUpdateException ex)
            {
                // На случай, если параллельный запрос занял email/phone в момент между проверкой и сохранением
                return BadRequest("Update failed. Possible conflict: " + ex.InnerException?.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserRegisterModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest("Пользователь с таким email уже существует");

            if (!string.IsNullOrEmpty(model.PhoneNumber) &&
                await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
                return BadRequest("Пользователь с таким номером телефона уже существует");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FcmToken = model.FmcToken,
                Password = model.Password,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Username || u.PhoneNumber == model.Username);

            if (user == null)
                return Unauthorized(new { Message = "Пользователь не найден" });

            if (user.Password != model.Password)
                return Unauthorized(new { Message = "Неверный пароль" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                user.UserId,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Email,
                IsCourier = user.Courier,
                IsAdmin = user.Admin
            });
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            // Если пользователь не найден, создаем нового
            if (user == null)
            {
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    PhoneNumber = request.PhoneNumber,
                    Password = "default_password", // Здесь следует добавить хэш реального пароля
                    Courier = false,
                    Admin = false
                    // Остальные поля можно оставить null или установить значения по умолчанию
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var code = new Random().Next(1000, 9999).ToString();
            var smsRequest = new
            {
                number = NormalizePhone(request.PhoneNumber),
                text = $"Ваш код подтверждения: {code}",
                sender = _config["SmsProNikitaKg:Sender"],
                sign = GenerateSign(request.PhoneNumber)
            };

            var response = await _httpClient.PostAsJsonAsync("sms/send", smsRequest);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Ошибка отправки SMS");

            var result = await response.Content.ReadFromJsonAsync<SmsSendResponse>();
            if (result?.Error != null)
                return BadRequest($"Ошибка: {result.Error}");

            return Ok(new { Message = "Код отправлен" });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            //var verifyRequest = new
            //{
            //    number = NormalizePhone(request.PhoneNumber),
            //    code = request.Code
            //};

            //var response = await _httpClient.PostAsJsonAsync("otp/verify", verifyRequest);
            //if (!response.IsSuccessStatusCode)
            //    return BadRequest("Ошибка проверки кода");

            //var result = await response.Content.ReadFromJsonAsync<OtpVerifyResponse>();
            //if (result?.Status != "success")
            //    return BadRequest("Неверный код подтверждения");

            //var user = await _context.Users
            //    .FirstAsync(u => u.PhoneNumber == request.PhoneNumber);


            const string testCode = "0000";

            // Проверяем код
            if (request.Code != testCode)
                return BadRequest("Неверный код подтверждения");

            // Ищем пользователя
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            // Если пользователь не найден, создаем нового
            if (user == null)
            {
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    PhoneNumber = request.PhoneNumber,
                    Password= request.PhoneNumber,
                    Courier = false,
                    Admin = false
                    // Остальные поля можно оставить null или установить значения по умолчанию
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                Token = token,
                user.UserId,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Email,
                IsCourier = user.Courier,
                IsAdmin = user.Admin
            });
        }

        private static string NormalizePhone(string phone)
        {
            return phone.StartsWith("996") ? phone : $"996{phone[^9..]}";
        }

        private string GenerateSign(string phoneNumber)
        {
            var login = _config["SmsProNikitaKg:Login"];
            var password = _config["SmsProNikitaKg:Password"];
            var input = $"{login}{password}{NormalizePhone(phoneNumber)}";

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.PhoneNumber),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, "User")
            };

            if (user.Admin == true)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            if (user.Courier == true)
                claims.Add(new Claim(ClaimTypes.Role, "Courier"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: _timeService.Now.AddDays(Convert.ToDouble(_config["Jwt:ExpireDays"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPatch("{id}/online-status")]
        public async Task<IActionResult> UpdateOnlineStatus(Guid id, [FromBody] UpdateOnlineStatusRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found");

            user.IsOnline = request.IsOnline;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                UserId = user.UserId,
                IsOnline = user.IsOnline
            });
        }

        // Модель запроса для изменения онлайн-статуса
        public class UpdateOnlineStatusRequest
        {
            public bool IsOnline { get; set; }
        }
    }

    public class SmsSendResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class OtpVerifyResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SendOtpRequest
    {
        public required string PhoneNumber { get; set; }
    }

    public class VerifyOtpRequest
    {
        public required string PhoneNumber { get; set; }
        public required string Code { get; set; }
    }
}