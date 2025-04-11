using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Product
    {
        public Guid ProductId { get; set; } // Уникальный идентификатор товара

        [DisplayName("Дата добавления")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ProductDate { get; set; }
        public required string Name { get; set; } // Название товара
        public string? Description { get; set; } // Описание товара
        public int? Stok { get; set; } // Количество товара
        public decimal PurchasePrice { get; set; } // Закупочная цена товара 40
        public decimal RetailPrice { get; set; } // Розничная цена товара 50

        [Range(0, double.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
        public decimal? DiscountPrice { get; set; } // Скидочная цена товара 45

        [NotMapped]
        public decimal FinalPrice => DiscountPrice.HasValue ? DiscountPrice.Value : RetailPrice; //FinalPrice, которое всегда возвращает актуальную цену

        [DisplayName("Показывать на главной")]
        public bool ShowOnHomePage { get; set; } = false;//ana sayfaya göster

        [DisplayName("Порядок на главной")] //Sıraya göre ana sayfaya göster
        public int? HomePageDisplayOrder { get; set; }

        public Guid CategoryId { get; set; } // Идентификатор категории, к которой принадлежит товар

        [JsonIgnore]
        public virtual Category? Category { get; set; } // Навигационное свойство для категории товара

        public virtual ICollection<ProductPhoto>? ProductPhotos { get; set; }
    }
    public class ProductPhoto
    {
        public Guid ProductPhotoId { get; set; }

        [DisplayName("Путь к файлу")]
        [StringLength(1200)]
        public required string PhotoPath { get; set; }
        public Guid ProductId { get; set; }

        [JsonIgnore]
        public virtual Product? Products { get; set; }
    }
}
