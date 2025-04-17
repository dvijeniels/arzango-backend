using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class CartItem
    {
        public Guid CartItemId { get; set; } // Уникальный идентификатор позиции корзины
        public Guid CartId { get; set; } // Идентификатор корзины

        [JsonIgnore]
        public virtual Cart? Cart { get; set; } // Навигационное свойство для корзины
        public Guid ProductId { get; set; } // Идентификатор товара

        [JsonIgnore]
        public virtual Product? Product { get; set; } // Навигационное свойство для товара
        public int Quantity { get; set; } // Количество товара в корзине 
        public decimal Price { get; set; } // Цена товара в корзине
    }

}
