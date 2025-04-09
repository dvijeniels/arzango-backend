namespace ArzanGo.Models
{
    public class Courier
    {
        public Guid CourierId { get; set; } // Уникальный идентификатор пользователя
        public string FirstName { get; set; } // Имя пользователя
        public string LastName { get; set; } // Фамилия пользователя
        public required string PhoneNumber { get; set; } // Номер телефона
        public string? Email { get; set; } // Электронная почта
        public required string Password { get; set; } // Хэш пароля
        public virtual List<Order>? Orders { get; set; } // Список принявших заказов 
        public virtual List<Cart>? Carts { get; set; } // Список принявших заказов 
    }
}
