using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateReport(
    [FromQuery] Guid categoryId,
    [FromQuery] Guid paymentMethodId,
    [FromQuery] string period = "weekly",
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
        {
            try
            {
                // Устанавливаем даты по умолчанию, если не указаны
                if (!startDate.HasValue || !endDate.HasValue)
                {
                    endDate = DateTime.Today;
                    startDate = endDate.Value.AddDays(-7);
                }

                // Получаем данные из базы
                var reportData = await GenerateReportData(
                    period,
                    startDate.Value,
                    endDate.Value,
                    categoryId,
                    paymentMethodId,
                    pageNumber,
                    pageSize);

                return Ok(new
                {
                    success = true,
                    message = "Rapor başarıyla oluşturuldu",
                    timestamp = DateTime.Now.ToString("o"),
                    data = reportData,
                    pagination = new
                    {
                        pageNumber,
                        pageSize,
                        totalRecords = reportData!.ProductAnalyses!.Count + reportData!.CategoryAnalyses!.Count,
                        totalPages = (int)Math.Ceiling((double)(reportData.ProductAnalyses.Count + reportData.CategoryAnalyses.Count) / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Rapor oluşturulurken hata oluştu: {ex.Message}",
                    timestamp = DateTime.Now.ToString("o")
                });
            }
        }
        private async Task<ReportData> GenerateReportData(
    string period,
    DateTime startDate,
    DateTime endDate,
    Guid categoryId,
    Guid paymentMethodId,
    int pageNumber,
    int pageSize)
        {
            // Основной запрос для фильтрации данных
            var query = _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .AsQueryable();

            // Check for default Guid instead of string.IsNullOrEmpty
            if (categoryId != Guid.Empty)
            {
                query = query.Where(o => o.OrderItems!.Any(oi => oi.Product!.CategoryId == categoryId));
            }

            // Check for default Guid instead of string.IsNullOrEmpty
            if (paymentMethodId != Guid.Empty)
            {
                query = query.Where(o => o.PaymentSettingId == paymentMethodId);
            }

            var orders = await query
                .Include(o => o.OrderItems!)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p!.Category)
                .Include(o => o.PaymentSettings)
                .ToListAsync();

            // Генерация отчета
            var reportData = new ReportData
            {
                Filters = new ReportFilters
                {
                    Period = period,
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd"),
                    CategoryId = categoryId,
                    PaymentMethodId = paymentMethodId
                },
                GeneralSummary = await GenerateGeneralSummary(orders),
                SalesTrends = GenerateSalesTrends(orders, startDate, endDate, period),
                ProductAnalyses = await GenerateProductAnalyses(orders, pageNumber, pageSize),
                CategoryAnalyses = await GenerateCategoryAnalyses(orders, pageNumber, pageSize),
                PaymentMethods = await GeneratePaymentMethods(orders) // Made async to match the new version
            };

            return reportData;
        }

        private static async Task<GeneralSummary> GenerateGeneralSummary(List<Order> orders)
        {
            return await Task.Run(() =>
            {
                var totalRevenue = orders.Sum(o => o.TotalAmount);
                var totalOrders = orders.Count;
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                var topProduct = orders
                    .SelectMany(o => o.OrderItems!)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.Name })
                    .Select(g => new
                    {
                        g.Key.ProductId,
                        ProductName = g.Key.Name,
                        SalesCount = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.SalesCount)
                    .FirstOrDefault();

                var topCategory = orders
                    .SelectMany(o => o.OrderItems!)
                    .GroupBy(oi => new { oi.Product!.CategoryId, oi.Product!.Category!.Name })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        CategoryName = g.Key.Name,
                        Revenue = g.Sum(oi => oi.Quantity * oi.Price),
                        RevenueRatio = (decimal)g.Sum(oi => oi.Quantity * oi.Price) / totalRevenue
                    })
                    .OrderByDescending(x => x.Revenue)
                    .FirstOrDefault();

                var topPaymentMethod = orders
                    .GroupBy(o => new { o.PaymentSettingId, o.PaymentSettings!.Name })
                    .Select(g => new
                    {
                        PaymentMethodId = g.Key.PaymentSettingId,
                        PaymentMethodName = g.Key.Name,
                        UsageCount = g.Count(),
                        UsageRatio = (decimal)g.Count() / totalOrders
                    })
                    .OrderByDescending(x => x.UsageCount)
                    .FirstOrDefault();

                return new GeneralSummary
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = averageOrderValue,
                    TopSellingProduct = topProduct != null ? new TopSellingProduct
                    {
                        ProductId = topProduct.ProductId,
                        ProductName = topProduct.ProductName,
                        SalesCount = topProduct.SalesCount
                    } : null,
                    MostPopularCategory = topCategory != null ? new MostPopularCategory
                    {
                        CategoryId = topCategory.CategoryId,
                        CategoryName = topCategory.CategoryName,
                        RevenueRatio = (double)topCategory.RevenueRatio
                    } : null,
                    MostUsedPayment = topPaymentMethod != null ? new MostUsedPayment
                    {
                        PaymentMethodId = topPaymentMethod.PaymentMethodId,
                        PaymentMethodName = topPaymentMethod.PaymentMethodName,
                        UsageRatio = (double)topPaymentMethod.UsageRatio
                    } : null
                };
            });
        }


        private static List<SalesTrend> GenerateSalesTrends(List<Order> orders, DateTime startDate, DateTime endDate, string period)
        {
            var trends = new List<SalesTrend>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var dailyOrders = orders
                    .Where(o => o.OrderDate.Date == currentDate.Date)
                    .ToList();

                var topProduct = dailyOrders
                    .SelectMany(o => o.OrderItems!)
                    .GroupBy(oi => oi.Product!.Name)
                    .Select(g => new
                    {
                        ProductName = g.Key,
                        SalesCount = g.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(x => x.SalesCount)
                    .FirstOrDefault();

                trends.Add(new SalesTrend
                {
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    Revenue = dailyOrders.Sum(o => o.TotalAmount),
                    OrderCount = dailyOrders.Count,
                    TopSellingProduct = topProduct!.ProductName
                });

                currentDate = period.ToLower() switch
                {
                    "daily" => currentDate.AddDays(1),
                    "weekly" => currentDate.AddDays(7),
                    "monthly" => currentDate.AddMonths(1),
                    _ => currentDate.AddDays(1)
                };
            }

            return trends;
        }

        private async Task<List<ProductAnalysis>> GenerateProductAnalyses(List<Order> orders, int pageNumber, int pageSize)
        {
            // First get all product analyses without the PreviousPeriodComparison
            var productAnalyses = orders
                .SelectMany(o => o.OrderItems!)
                .GroupBy(oi => new {
                    oi.ProductId,
                    ProductName = oi.Product?.Name,
                    CategoryName = oi.Product?.Category?.Name
                })
                .Select(g => new ProductAnalysis
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName!,
                    CategoryName = g.Key.CategoryName!,
                    SalesCount = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price),
                    AveragePrice = g.Average(oi => oi.Price),
                    StockStatus = 0, // Temporary value, will be updated later
                    PreviousPeriodComparison = 0 // Temporary value
                })
                .OrderByDescending(pa => pa.TotalRevenue)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Now update each analysis with stock status and previous period comparison
            foreach (var analysis in productAnalyses)
            {
                // Get stock status
                var product = await _context.Products.FindAsync(analysis.ProductId);
                if (product != null)
                {
                    analysis.StockStatus = product.Stock;
                }

                // Calculate previous period comparison
                var currentStartDate = orders.Min(o => o.OrderDate);
                var currentEndDate = orders.Max(o => o.OrderDate);
                analysis.PreviousPeriodComparison = await CalculatePreviousPeriodComparison(
                    analysis.ProductId,
                    currentStartDate,
                    currentEndDate);
            }

            return productAnalyses;
        }

        private async Task<decimal> CalculatePreviousPeriodComparison(Guid productId, DateTime currentStartDate, DateTime currentEndDate)
        {
            // Calculate previous period dates (same duration as current period)
            var periodDuration = currentEndDate - currentStartDate;
            var previousStartDate = currentStartDate - periodDuration;
            var previousEndDate = currentStartDate;

            // Get previous period sales
            var previousSales = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .Where(oi => oi.Order!.OrderDate >= previousStartDate && oi.Order.OrderDate < previousEndDate)
                .SumAsync(oi => oi.Quantity * oi.Price);

            // Get current period sales (from the orders we already have)
            var currentSales = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .Where(oi => oi.Order!.OrderDate >= currentStartDate && oi.Order.OrderDate <= currentEndDate)
                .SumAsync(oi => oi.Quantity * oi.Price);

            if (previousSales == 0) return currentSales > 0 ? 1.0m : 0m;

            return (currentSales - previousSales) / previousSales;
        }

        private async Task<List<CategoryAnalysis>> GenerateCategoryAnalyses(List<Order> orders, int pageNumber, int pageSize)
        {
            // Convert totalRevenue to double for consistent division
            var totalRevenue = (double)orders.Sum(o => o.TotalAmount);

            // Create a list to hold our category analyses
            var categoryAnalyses = new List<CategoryAnalysis>();

            // Process in batches if you have large datasets
            var groupedItems = orders
                .Where(o => o.OrderItems != null)
                .SelectMany(o => o.OrderItems!)
                .Where(oi => oi.Product != null && oi.Product.Category != null)
                .GroupBy(oi => new
                {
                    oi.Product!.CategoryId,
                    oi.Product.Category!.Name
                })
                .ToList();

            foreach (var group in groupedItems)
            {
                // Get product count asynchronously
                var productCount = await _context.Products
                    .Where(p => p.CategoryId == group.Key.CategoryId)
                    .CountAsync();

                // Calculate total revenue for the group
                var totalRevenueForGroup = group.Sum(oi => oi.Quantity * oi.Price);

                // Determine top selling product
                var topProduct = await Task.Run(() =>
                    group
                        .GroupBy(oi => oi.Product!.Name)
                        .Select(gr => new { Name = gr.Key, Count = gr.Sum(x => x.Quantity) })
                        .OrderByDescending(x => x.Count)
                        .FirstOrDefault());

                categoryAnalyses.Add(new CategoryAnalysis
                {
                    CategoryId = group.Key.CategoryId,
                    CategoryName = group.Key.Name,
                    ProductCount = productCount,
                    TotalRevenue = totalRevenueForGroup,
                    RevenueRatio = totalRevenue > 0 ?
                        (double)totalRevenueForGroup / totalRevenue :
                        0,
                    TotalOrders = group.Select(oi => oi.OrderId).Distinct().Count(),
                    TopSellingProduct = topProduct?.Name ?? string.Empty
                });
            }

            // Apply pagination
            return categoryAnalyses
                .OrderByDescending(ca => ca.TotalRevenue)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        private static async Task<List<PaymentMethodAnalysis>> GeneratePaymentMethods(List<Order> orders)
        {
            return await Task.Run(() =>
            {
                var totalOrders = orders.Count;

                return orders
                    .GroupBy(o => new {
                        PaymentMethodId = o.PaymentSettings!.PaymentSettingId,
                        PaymentMethodName = o.PaymentSettings.Name
                    })
                    .Select(g => new PaymentMethodAnalysis
                    {
                        PaymentMethodId = g.Key.PaymentMethodId,
                        PaymentMethodName = g.Key.PaymentMethodName,
                        UsageCount = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalAmount),
                        AverageAmount = g.Average(o => o.TotalAmount),
                        UsageRatio = (double)g.Count() / totalOrders
                    })
                    .OrderByDescending(pm => pm.UsageCount)
                    .ToList();
            });
        }

    }

    // Модели данных
    public class ReportData
    {
        public ReportFilters? Filters { get; set; }
        public GeneralSummary? GeneralSummary { get; set; }
        public List<SalesTrend>? SalesTrends { get; set; }
        public List<ProductAnalysis>? ProductAnalyses { get; set; }
        public List<CategoryAnalysis>? CategoryAnalyses { get; set; }
        public List<PaymentMethodAnalysis>? PaymentMethods { get; set; }
    }

    public class ReportFilters
    {
        public required string Period { get; set; }
        public required string StartDate { get; set; }
        public required string EndDate { get; set; }
        public Guid CategoryId { get; set; }
        public Guid PaymentMethodId { get; set; }
    }

    public class GeneralSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public TopSellingProduct? TopSellingProduct { get; set; }
        public MostPopularCategory? MostPopularCategory { get; set; }
        public MostUsedPayment? MostUsedPayment { get; set; }
    }

    public class TopSellingProduct
    {
        public Guid ProductId { get; set; }
        public required string ProductName { get; set; }
        public int SalesCount { get; set; }
    }

    public class MostPopularCategory
    {
        public Guid CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public double RevenueRatio { get; set; }
    }

    public class MostUsedPayment
    {
        public Guid PaymentMethodId { get; set; }
        public required string PaymentMethodName { get; set; }
        public double UsageRatio { get; set; }
    }

    public class SalesTrend
    {
        public required string Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public required string TopSellingProduct { get; set; }
    }

    public class ProductAnalysis
    {
        public Guid ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string CategoryName { get; set; }
        public int SalesCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int? StockStatus { get; set; }
        public decimal PreviousPeriodComparison { get; set; }
    }

    public class CategoryAnalysis
    {
        public Guid CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public double RevenueRatio { get; set; }
        public int TotalOrders { get; set; }
        public required string TopSellingProduct { get; set; }
    }

    public class PaymentMethodAnalysis
    {
        public Guid PaymentMethodId { get; set; }
        public required string PaymentMethodName { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public double UsageRatio { get; set; }
    }
}
