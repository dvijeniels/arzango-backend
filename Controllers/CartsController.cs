using ArzanGo.Data;
using ArzanGo.Models;
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
        [HttpPost]
        public async Task<ActionResult<Cart>> CreateCart(Cart cart)
        {
            cart.CartId = Guid.NewGuid();
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCart), new { id = cart.CartId }, cart);
        }

        // ✅ Обновить корзину
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCart(Guid id, Cart cart)
        {
            if (id != cart.CartId)
                return BadRequest();

            _context.Entry(cart).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Carts.Any(e => e.CartId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить корзину
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(Guid id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
                return NotFound();

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
