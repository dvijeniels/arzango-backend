using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouriersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CouriersController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить всех курьеров
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Courier>>> GetCouriers()
        {
            return await _context.Couriers.Include(c => c.Orders)
                                          .Include(c => c.Carts)
                                          .ToListAsync();
        }

        // ✅ Получить одного курьера по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Courier>> GetCourier(Guid id)
        {
            var courier = await _context.Couriers.Include(c => c.Orders)
                                                 .Include(c => c.Carts)
                                                 .FirstOrDefaultAsync(c => c.CourierId == id);

            if (courier == null)
                return NotFound();

            return courier;
        }

        // ✅ Создать нового курьера
        [HttpPost]
        public async Task<ActionResult<Courier>> CreateCourier(Courier courier)
        {
            courier.CourierId = Guid.NewGuid(); // Генерируем новый ID
            _context.Couriers.Add(courier);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourier), new { id = courier.CourierId }, courier);
        }

        // ✅ Обновить данные курьера
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourier(Guid id, Courier courier)
        {
            if (id != courier.CourierId)
                return BadRequest();

            _context.Entry(courier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Couriers.Any(e => e.CourierId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить курьера
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourier(Guid id)
        {
            var courier = await _context.Couriers.FindAsync(id);
            if (courier == null)
                return NotFound();

            _context.Couriers.Remove(courier);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
