using System.ComponentModel.DataAnnotations;

namespace ArzanGo.DTO
{
    public class UpdateProductDto
    {
        public Guid ProductId { get; set; }
        public required string Name { get; set; }

        public string? Description { get; set; }

        public int? Stock { get; set; } // Количество товара

        public bool ShowOnHomePage { get; set; } = false;//ana sayfaya göster

        [Required]
        public decimal PurchasePrice { get; set; }

        [Required]
        public decimal RetailPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        public IFormFileCollection? Photos { get; set; }
    }
}
