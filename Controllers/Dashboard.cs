using ArzanGo.Data;
using ArzanGo.DTO;
using ArzanGo.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [CustomAuthorize(Roles = "Admin")]
    public class Dashboard : ControllerBase
    {
        private readonly AppDbContext _context;
        public Dashboard(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard-data")]
        public async Task<ActionResult<DashboardDataResponse>> GetDashboardData()
        {
            // Получаем текущую дату и вычисляем дату 4 месяца назад
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-3); // Чтобы получить 4 месяца (включая текущий)

            // Получаем данные о продажах за последние 4 месяца
            var salesData = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => new { o.OrderDate.Month, o.OrderDate.Year })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    TotalSales = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Форматируем данные для ответа
            var formattedSalesData = salesData.Select(s => new SalesData
            {
                Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(s.Month),
                Sales = s.TotalSales
            }).ToList();

            // Если данных меньше чем 4 месяца, добавляем недостающие месяцы с нулевыми продажами
            for (int i = 0; i < 4; i++)
            {
                var date = startDate.AddMonths(i);
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month);

                if (!formattedSalesData.Any(s => s.Month == monthName))
                {
                    formattedSalesData.Insert(i, new SalesData { Month = monthName, Sales = 0 });
                }
            }

            // Оставляем только последние 4 месяца
            formattedSalesData = formattedSalesData.TakeLast(4).ToList();

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
                SalesData = formattedSalesData,
                CategoryData = categoryData,
                GrandTotal = grandTotal,
                StockProducts = stockProducts
            };
        }
    }
}

