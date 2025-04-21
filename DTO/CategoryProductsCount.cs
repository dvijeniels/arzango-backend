namespace ArzanGo.DTO
{
    public class CategoryProductsCount
    {
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int ProductsCount { get; set; }
    }
}
