namespace ArzanGo.Models
{
    public class User
    {
        public Guid UserId { get; set; } // Уникальный идентификатор пользователя
        public string? FirstName { get; set; } // Имя пользователя
        public string? LastName { get; set; } // Фамилия пользователя
        public string PhoneNumber { get; set; } // Номер телефона
        public string? Email { get; set; } // Электронная почта
        public string Password { get; set; } // Хэш пароля
        public virtual List<Order> Orders { get; set; } // Список заказов пользователя
        public virtual List<Cart> Carts { get; set; } 
        public virtual ICollection<Favorite> Favorites { get; set; } = new HashSet<Favorite>();
        public List<Address> ShippingAddresses { get; set; }
    }
}
