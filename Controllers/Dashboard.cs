using ArzanGo.Data;
using ArzanGo.DTO;
using ArzanGo.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
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
                    ProductsCount = c.Products != null ? c.Products.Count : 0
                })
                .ToListAsync();

            return new ProductsByCategoriesResponse
            {
                TotalCategories = result.Count,
                TotalProducts = result.Sum(x => x.ProductsCount),
                Categories = result
            };
        }

        [HttpGet("dashboard-data")]
        public async Task<ActionResult<DashboardDataResponse>> GetDashboardData()
        {
            // Sales data (примерные данные - нужно адаптировать под вашу БД)
            var salesData = new List<SalesData>
            {
                new SalesData { Month = "Янв", Sales = 4000 },
                new SalesData { Month = "Фев", Sales = 3000 },
                new SalesData { Month = "Мар", Sales = 5000 },
                new SalesData { Month = "Апр", Sales = 7000 }
            };

            // Category data
            var categoryData = await _context.Categories
                .Include(c => c.Products)
                .Select(c => new CategoryData
                {
                    Name = c.Name,
                    Value = c.Products!.Count
                })
                .ToListAsync();

            // Grand totals
            var grandTotal = new List<GrandTotal>
            {
                new GrandTotal
                {
                    Icon = "TrendingUp",
                    Label = "Жалпы сатылым",
                    Value = _context.Orders.Sum(o=>o.TotalAmount).ToString()
                },
                new GrandTotal
                {
                    Icon = "Inventory",
                    Label = "Жалпы продукция",
                    Value = _context.Products.Count().ToString()
                },
                new GrandTotal
                {
                    Icon = "Category",
                    Label = "Категориялардын саны",
                    Value = _context.Categories.Count().ToString()
                },
                new GrandTotal
                {
                    Icon = "People",
                    Label = "Колдонуучулардын саны",
                    Value = _context.Users.Where(u=>u.Courier!=true && u.Admin!=true).Count().ToString("N0")
                },
                new GrandTotal
                {
                    Icon = "SupportAgent",
                    Label = "Активдүү курьерлер",
                    Value = _context.Users.Count(c => c.Courier==true).ToString()
                },
                new GrandTotal
                {
                    Icon = "ShoppingCart",
                    Label = "Кайтаруу суранычтары",
                    Value = _context.Orders.Count(o => o.Status == Models.Status.Canceled).ToString()
                }
            };

            // Stock products (пример - нужно адаптировать под вашу модель)
            var stockProducts = await _context.Products
                .OrderBy(p => p.Stock)
                .Take(3)
                .Select(p => new StockProduct
                {
                    Name = p.Name,
                    Stock = p.Stock
                })
                .ToListAsync();

            return new DashboardDataResponse
            {
                SalesData = salesData,
                CategoryData = categoryData,
                GrandTotal = grandTotal,
                StockProducts = stockProducts
            };
        }
    }
}

