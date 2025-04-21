using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить избранные товары пользователя
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites(Guid userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .ThenInclude(p => p != null ? p.ProductPhotos : null)
                .ToListAsync();
        }

        // ✅ Добавить товар в избранное
        [HttpPost]
        public async Task<ActionResult<Favorite>> AddToFavorites(Favorite favorite)
        {
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFavorites), new { userId = favorite.UserId }, favorite);
        }

        // ✅ Удалить товар из избранного
        [HttpDelete("{favoriteId}")]
        public async Task<IActionResult> RemoveFromFavorites(Guid favoriteId)
        {
            var favorite = await _context.Favorites.FindAsync(favoriteId);
            if (favorite == null)
                return NotFound();

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
