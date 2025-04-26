using ArzanGo.Data;
using ArzanGo.DTO;
using ArzanGo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    public class AddressesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AddressesController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Получить все адреса пользователя
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<AddressDto>>> GetUserAddresses(Guid userId)
        {
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var addressDtos = addresses.Select(a => new AddressDto
            {
                AddressId = a.AddressId,
                City = GetEnumDisplayName(a.City),
                Street = a.Street,
                House = a.House,
                Additionally = a.Additionally,
                PostalCode = a.PostalCode,
                UserId = a.UserId
            });

            return Ok(addressDtos);
        }
        public static string GetEnumDisplayName(Enum value)
        {
            return value.GetType()
                        .GetMember(value.ToString())
                        .First()
                        .GetCustomAttribute<DisplayAttribute>()?
                        .GetName() ?? value.ToString();
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
        public async Task<ActionResult<Address>> AddAddress([FromBody] Address address)
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
