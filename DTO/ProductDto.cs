using System.ComponentModel.DataAnnotations;

namespace ArzanGo.DTO
{
    public class ProductDto
    {
        public required string Name { get; set; }

        public string? Description { get; set; }

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
