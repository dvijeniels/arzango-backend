using static ArzanGo.Controllers.Dashboard;

namespace ArzanGo.DTO
{
    public class ProductsByCategoriesResponse
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public List<CategoryProductsCount> Categories { get; set; } = new();
    }
}
