using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class User
    {
        public Guid UserId { get; set; } // Уникальный идентификатор пользователя
        public string? FirstName { get; set; } // Имя пользователя
        public string? LastName { get; set; } // Фамилия пользователя
        public required string PhoneNumber { get; set; } // Номер телефона
        public string? Email { get; set; } // Электронная почта
        public bool? Courier { get; set; } // Является ли Курьером?
        public bool? Admin { get; set; } // Является ли Админом?
        public record Position(double Latitude, double Longitude);
        public required string Password { get; set; } // Хэш пароля
        public double? Raiting { get; set; }

        [JsonIgnore]
        public virtual List<Order>? Orders { get; set; } // Список заказов пользователя

        [JsonIgnore]
        public virtual List<Cart>? Carts { get; set; }

        [JsonIgnore]
        public virtual ICollection<Favorite>? Favorites { get; set; } = new HashSet<Favorite>();

        [JsonIgnore]
        public List<Address>? ShippingAddresses { get; set; }

        public string? FcmToken { get; set; }
    }
}
