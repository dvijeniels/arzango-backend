namespace ArzanGo.Models.Requests
{
    public class DashboardDataResponse
    {
        public required List<SalesData> SalesData { get; set; }
        public required List<CategoryData> CategoryData { get; set; }
        public required List<GrandTotal> GrandTotal { get; set; }
        public required List<StockProduct> StockProducts { get; set; }
    }

    public class SalesData
    {
        public required string Month { get; set; }
        public decimal Sales { get; set; }
    }

    public class CategoryData
    {
        public required string Name { get; set; }
        public int Value { get; set; }
    }

    public class GrandTotal
    {
        public required string Icon { get; set; }
        public required string Label { get; set; }
        public required string Value { get; set; }
    }

    public class StockProduct
    {
        public required string Name { get; set; }
        public int? Stock { get; set; }
    }
}
