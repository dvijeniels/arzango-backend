using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AboutController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // ✅ Получить информацию "О нас"
        [HttpGet]
        public async Task<ActionResult<About?>> GetAbout()
        {
            var about = await _context.About.FirstOrDefaultAsync();
            return about == null ? NotFound("About information not found") : about;
        }

        // ✅ Обновить информацию "О нас"
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAbout(Guid id, About about)
        {
            if (id != about.AboutId)
                return BadRequest();

            _context.Entry(about).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.About.Any(a => a.AboutId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }
    }
}
