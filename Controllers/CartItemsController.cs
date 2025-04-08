using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArzanGo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArzanGo.Data;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartItemsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить все товары в корзинах
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCartItems()
        {
            return await _context.CartItems.Include(ci => ci.Cart)
                                           .Include(ci => ci.Product)
                                           .ToListAsync();
        }

        // ✅ Получить товар в корзине по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItem>> GetCartItem(Guid id)
        {
            var cartItem = await _context.CartItems.Include(ci => ci.Cart)
                                                   .Include(ci => ci.Product)
                                                   .FirstOrDefaultAsync(ci => ci.CartItemId == id);

            if (cartItem == null)
                return NotFound();

            return cartItem;
        }

        // ✅ Добавить товар в корзину
        [HttpPost]
        public async Task<ActionResult<CartItem>> CreateCartItem(CartItem cartItem)
        {
            cartItem.CartItemId = Guid.NewGuid();
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCartItem), new { id = cartItem.CartItemId }, cartItem);
        }

        // ✅ Обновить товар в корзине
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(Guid id, CartItem cartItem)
        {
            if (id != cartItem.CartItemId)
                return BadRequest();

            _context.Entry(cartItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CartItems.Any(e => e.CartItemId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить товар из корзины
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(Guid id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
                return NotFound();

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
