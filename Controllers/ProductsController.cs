using ArzanGo.Data;
using ArzanGo.DTO;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.Include(p => p.Category).Include(p=>p.ProductPhotos).ToListAsync();
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(Guid id)
        {
            var product = await _context.Products.Include(p => p.Category)
                                                 .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct([FromForm] ProductDto productDto)
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrEmpty(productDto.Name))
                    return BadRequest("Название продукта обязательно");

                if (productDto.CategoryId == Guid.Empty)
                    return BadRequest("Категория обязательна");

                // Создаем новый продукт
                var product = new Product
                {
                    ProductId = Guid.NewGuid(),
                    Name = productDto.Name,
                    Description = productDto.Description,
                    PurchasePrice = productDto.PurchasePrice,
                    RetailPrice = productDto.RetailPrice,
                    DiscountPrice = productDto.DiscountPrice,
                    CategoryId = productDto.CategoryId,
                    ProductDate = DateTime.Now
                };

                _context.Products.Add(product);

                // Обрабатываем загруженные файлы
                if (productDto.Photos != null && productDto.Photos.Count > 0)
                {
                    product.ProductPhotos = new List<ProductPhoto>();

                    // Путь к папке для загрузки
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    foreach (var photo in productDto.Photos)
                    {
                        try
                        {
                            // Валидация файла
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

                            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                            {
                                return BadRequest($"Недопустимый формат файла: {photo.FileName}");
                            }

                            if (photo.Length == 0)
                                continue;

                            if (photo.Length > 5 * 1024 * 1024) // 5MB
                                return BadRequest($"Файл слишком большой: {photo.FileName}");

                            // Генерируем уникальное имя файла
                            var fileName = $"{Guid.NewGuid()}{extension}";
                            var filePath = Path.Combine(uploadPath, fileName);

                            // Сохраняем файл на сервер
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await photo.CopyToAsync(stream);
                            }

                            // Добавляем запись о фото в БД
                            product.ProductPhotos.Add(new ProductPhoto
                            {
                                ProductPhotoId = Guid.NewGuid(),
                                PhotoPath = $"/images/products/{fileName}",
                                ProductId = product.ProductId
                            });
                        }
                        catch (Exception ex)
                        {
                            // Логируем ошибку, но продолжаем обработку других файлов
                            Console.WriteLine($"Ошибка при обработке файла {photo.FileName}: {ex.Message}");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка при создании продукта: {ex}");
                return StatusCode(500, "Произошла ошибка при создании продукта");
            }
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(Guid id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.ProductPhotos)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Удаляем все связанные фотографии
            foreach (var photo in product.ProductPhotos)
            {
                var filePath = Path.Combine("wwwroot", photo.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.ProductPhotos.Remove(photo);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        // PUT: api/products/{id}/photos
        [HttpPut("{id}/photos")]
        public async Task<IActionResult> UpdateProductPhotos(Guid id, [FromForm] List<IFormFile> photos)
        {
            var product = await _context.Products
                .Include(p => p.ProductPhotos)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Удаляем старые фото (опционально)
            foreach (var photo in product.ProductPhotos)
            {
                var filePath = Path.Combine("wwwroot", photo.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _context.ProductPhotos.Remove(photo);
            }

            // Добавляем новые фото
            if (photos != null && photos.Count > 0)
            {
                product.ProductPhotos = new List<ProductPhoto>();

                foreach (var photo in photos)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        return BadRequest("Недопустимый формат файла");
                    }

                    if (photo.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine("wwwroot/images/products", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        product.ProductPhotos.Add(new ProductPhoto
                        {
                            ProductPhotoId = Guid.NewGuid(),
                            PhotoPath = $"/images/products/{fileName}",
                            ProductId = product.ProductId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Фотографии обновлены");
        }

    }
}
