using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Category
    {
        public Guid CategoryId { get; set; } // Уникальный идентификатор категории
        public string Name { get; set; } // Название категории
        public string? Description { get; set; } // Описание категории

        [JsonIgnore]
        public List<Product>? Products { get; set; } // Список товаров, принадлежащих категории
    }

}
