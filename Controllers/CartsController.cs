using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить все корзины
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCarts()
        {
            return await _context.Carts.Include(c => c.User)
                                       .Include(c => c.CartItems!)
                                       .ThenInclude(p=>p.Product)
                                       .ToListAsync();
        }

        // ✅ Получить корзину по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(Guid id)
        {
            var cart = await _context.Carts.Include(c => c.User)
                                           .Include(c => c.CartItems)
                                           .FirstOrDefaultAsync(c => c.CartId == id);

            if (cart == null)
                return NotFound();

            return cart;
        }

        // ✅ Создать новую корзину
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<Cart>> GetUserCart(Guid userId) // bu metodla kontrol ediyoruz kullanicinin carti var mi? yoksa olusturuyoruz
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Создаем новую корзину, если не найдена
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // Добавить товар в корзину
        [HttpPost("user/{userId}/items")]
        public async Task<ActionResult<Cart>> AddToCart(Guid userId, [FromBody] AddToCartRequest request)
        {
            // 1. Валидация запроса
            if (request == null)
                return BadRequest("Request cannot be null");

            if (request.Quantity <= 0)
                return BadRequest("Quantity must be positive");

            try
            {
                // 2. Получаем или создаем корзину
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        CartId = Guid.NewGuid(),
                        UserId = userId,
                        CartItems = new List<CartItem>(),
                        TotalAmount = 0
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Инициализируем CartItems если null
                    cart.CartItems ??= new List<CartItem>();
                }

                // 3. Проверяем товар
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound("Product not found");

                // 5. Ищем товар в корзине
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CartItemId = Guid.NewGuid(),
                        CartId = cart.CartId,
                        ProductId = product.ProductId,
                        Quantity = request.Quantity,
                        Price = product.FinalPrice
                    };
                    _context.CartItems.Add(newItem);
                    cart.CartItems.Add(newItem);
                }

                // 6. Пересчитываем сумму (с защитой от null)
                cart.TotalAmount = cart.CartItems.Sum(ci => ci.Price * ci.Quantity);

                await _context.SaveChangesAsync();
                return Ok(cart);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        // Удалить товар из корзины
        [HttpDelete("user/{userId}/items/{productId}")]
        public async Task<ActionResult<Cart>> RemoveFromCart(Guid userId, Guid productId)
        {
            // 1. Находим корзину пользователя
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Корзина не найдена");

            // 2. Проверяем наличие товаров в корзине
            if (cart.CartItems == null || !cart.CartItems.Any())
                return NotFound("Корзина пуста");

            // 3. Находим удаляемый товар в корзине
            var itemToRemove = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (itemToRemove == null)
                return NotFound("Товар не найден в корзине");

            // 4. Удаляем товар из корзины
            _context.CartItems.Remove(itemToRemove);

            // 5. Пересчитываем общую сумму (исключая удаленный товар)
            cart.TotalAmount = cart.CartItems
                .Except(new[] { itemToRemove }) // Безопасный способ исключить элемент
                .Sum(ci => ci.Price * ci.Quantity);

            try
            {
                await _context.SaveChangesAsync();

                // Возвращаем обновленную корзину
                var updatedCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstAsync(c => c.CartId == cart.CartId);

                return Ok(updatedCart);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ошибка при удалении товара из корзины");
            }
        }
    }
}
