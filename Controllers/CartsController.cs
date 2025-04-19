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
                                       .Include(c => c.CartItems)
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
        public async Task<ActionResult<Cart>> AddToCart(Guid userId, [FromBody] AddToCartRequest request)// sonra bu metodla itemleri olusturuyoruz
        {
            // 1. Bir kullanıcının sepetini bulun veya oluşturun
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? new Cart { UserId = userId };

            if (cart.CartId == Guid.Empty)
            {
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // 2. Ürünü bulun
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return NotFound("Product not found");

            // 3. Sepette böyle bir ürün olup olmadığını kontrol edin
            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Ürün zaten sepetteyse miktarı artırın
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                // Yeni bir sepet öğesi oluştur
                var newItem = new CartItem
                {
                    CartItemId = Guid.NewGuid(),
                    CartId = cart.CartId,
                    ProductId = product.ProductId,
                    Quantity = request.Quantity,
                    Price = product.FinalPrice
                };

                cart.CartItems ??= new List<CartItem>();
                cart.CartItems.Add(newItem);
            }

            // 4. Toplam tutarı yeniden hesaplıyoruz
            cart.TotalAmount = cart.CartItems.Sum(ci => ci.Price * ci.Quantity);

            await _context.SaveChangesAsync();

            return Ok(cart);
        }

        // Удалить товар из корзины
        [HttpDelete("user/{userId}/items/{productId}")]
        public async Task<ActionResult<Cart>> RemoveFromCart(Guid userId, Guid productId)
        {
            // 1. Находим корзину пользователя
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            // 2. Находим удаляемый товар в корзине
            var itemToRemove = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == productId);
            if (itemToRemove == null) return NotFound("Item not found in cart");

            // 3. Удаляем товар из корзины
            _context.CartItems.Remove(itemToRemove);

            // 4. Пересчитываем общую сумму
            cart.TotalAmount = cart.CartItems
                .Where(ci => ci.CartItemId != itemToRemove.CartItemId)
                .Sum(ci => ci.Price * ci.Quantity);

            await _context.SaveChangesAsync();

            return Ok(cart);
        }
    }
}
