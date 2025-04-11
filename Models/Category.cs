using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Category
    {
        public Guid CategoryId { get; set; } // Уникальный идентификатор категории
        public required string Name { get; set; } // Название категории
        public string? Description { get; set; } // Описание категории
        public string? PhotoPath { get; set; } // Resim

        [JsonIgnore]
        public List<Product>? Products { get; set; } // Список товаров, принадлежащих категории
    }

}
