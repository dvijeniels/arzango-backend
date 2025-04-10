namespace ArzanGo.Models
{
    public class Cart //Корзина
    {
        public Guid CartId { get; set; } // Уникальный идентификатор корзины
        public Guid UserId { get; set; } // Идентификатор пользователя
        public virtual User? User { get; set; } // Навигационное свойство для пользователя
        public Guid CourierId { get; set; } // Идентификатор курьера
        public virtual List<CartItem>? CartItems { get; set; } // Список товаров в корзине
        public decimal TotalAmount { get; set; } // Общая сумма корзины
    }

}
