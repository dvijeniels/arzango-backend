using ArzanGo.Data;
using ArzanGo.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Dashboard : ControllerBase
    {
        private readonly AppDbContext _context;
        public Dashboard(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("products-by-categories")]
        public async Task<ActionResult<ProductsByCategoriesResponse>> GetProductsCountByCategories()
        {
            var result = await _context.Categories
                .Include(c => c.Products) // Подгружаем связанные продукты
                .Select(c => new CategoryProductsCount
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.Name,
                    ProductsCount = c.Products.Count
                })
                .ToListAsync();

            return new ProductsByCategoriesResponse
            {
                TotalCategories = result.Count,
                TotalProducts = result.Sum(x => x.ProductsCount),
                Categories = result
            };
        }
    }
}
