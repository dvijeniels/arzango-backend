using ArzanGo.Data;
using ArzanGo.DTO;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoriesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment= environment;
        }

        // ✅ Получить все категории
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.Include(c => c.Products).ToListAsync();
        }

        // ✅ Получить категорию по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(Guid id)
        {
            var category = await _context.Categories.Include(c => c.Products)
                                                    .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            return category;
        }

        // ✅ Создать новую категорию
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory([FromForm] CategoryDto dto)
        {
            var category = new Category
            {
                CategoryId = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description
            };

            if (dto.Photo != null)
            {
                category.PhotoPath = await SaveImage(dto.Photo);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }


        // ✅ Обновить категорию
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(
            Guid id,
            [FromForm] CategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = dto.Name;
            category.Description = dto.Description;

            if (dto.Photo != null)
            {
                // Удаляем старое фото если оно есть
                if (!string.IsNullOrEmpty(category.PhotoPath))
                {
                    DeleteImage(category.PhotoPath);
                }
                category.PhotoPath = await SaveImage(dto.Photo);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Categories.Any(e => e.CategoryId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // ✅ Удалить категорию
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            if (!string.IsNullOrEmpty(category.PhotoPath))
            {
                DeleteImage(category.PhotoPath);
            }
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<string?> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // Создаем папку если ее нет
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "category");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Генерируем уникальное имя файла
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return Path.Combine("images", "category", uniqueFileName).Replace("\\", "/");
        }

        private void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
