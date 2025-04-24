using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Request;
using ArzanGo.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public UsersController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ✅ Получить всех пользователей
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.Include(u => u.Orders)
                                       .Include(u => u.Favorites)
                                       .Include(u => u.ShippingAddresses)
                                       .ToListAsync();
        }

        // ✅ Получить одного пользователя по ID
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

        // ✅ Создать нового пользователя
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            user.UserId = Guid.NewGuid(); // Генерируем новый ID
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        // ✅ Обновить пользователя
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, User user)
        {
            if (id != user.UserId)
                return BadRequest();

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить пользователя
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
            // Проверка на существующего пользователя
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest("Пользователь с таким email уже существует");

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
                    return BadRequest("Пользователь с таким номером телефона уже существует");
            }

            // Создаем нового пользователя
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password, // В реальном проекте нужно хэшировать пароль!
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // 1. Находим пользователя по номеру телефона или email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Username || u.PhoneNumber == model.Username);

            if (user == null)
            {
                return Unauthorized(new { Message = "Пользователь не найден" });
            }

            // 2. Проверяем пароль (в реальном проекте используйте хэширование!)
            if (user.Password != model.Password) // Замените на проверку хэша в реальном проекте
            {
                return Unauthorized(new { Message = "Неверный пароль" });
            }

            // 3. Генерируем JWT токен с ролями
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                IsCourier = user.Courier,
                IsAdmin = user.Admin
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key не найден в конфигурации");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.PhoneNumber),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.UserId.ToString()),
            };

            // Добавляем роли в claims
            if (user.Admin == true)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            if (user.Courier == true)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Courier"));
            }
            // По умолчанию все пользователи - User
            claims.Add(new Claim(ClaimTypes.Role, "User"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireDays"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
