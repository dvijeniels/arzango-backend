using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class OrderItem
    {
        public Guid OrderItemId { get; set; } // Уникальный идентификатор позиции заказа
        public Guid OrderId { get; set; } // Идентификатор заказа

        [JsonIgnore]
        public virtual Order? Order { get; set; } // Навигационное свойство для заказа
        public Guid ProductId { get; set; } // Идентификатор товара

        [JsonPropertyName("product")]
        public virtual Product? Product { get; set; } // Навигационное свойство для товара
        public int Quantity { get; set; } // Количество товара в заказе
        public decimal Price { get; set; } // Цена товара на момент заказа
    }
}
