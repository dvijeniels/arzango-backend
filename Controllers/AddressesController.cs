using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AddressesController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить все адреса пользователя
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Address>>> GetUserAddresses(Guid userId)
        {
            return await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
        }

        // ✅ Получить один адрес по ID
        [HttpGet("details/{id}")]
        public async Task<ActionResult<Address>> GetAddress(Guid id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            return address;
        }

        // ✅ Добавить новый адрес
        [HttpPost]
        public async Task<ActionResult<Address>> AddAddress(Address address)
        {
            address.AddressId = Guid.NewGuid();
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddress), new { id = address.AddressId }, address);
        }

        // ✅ Обновить адрес
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(Guid id, Address address)
        {
            if (id != address.AddressId)
                return BadRequest();

            _context.Entry(address).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Addresses.Any(a => a.AddressId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить адрес
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
