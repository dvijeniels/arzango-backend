using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Cart //Корзина
    {
        public Guid CartId { get; set; } // Уникальный идентификатор корзины
        public Guid UserId { get; set; } // Идентификатор пользователя

        [JsonIgnore]
        public virtual User? User { get; set; } // Навигационное свойство для пользователя

        [JsonIgnore]
        public virtual List<CartItem>? CartItems { get; set; } // Список товаров в корзине
        public decimal TotalAmount { get; set; } // Общая сумма корзины
    }

}
