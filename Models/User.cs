using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class User
    {
        public Guid UserId { get; set; } // Уникальный идентификатор пользователя
        public string? FirstName { get; set; } // Имя пользователя
        public string? LastName { get; set; } // Фамилия пользователя

        [Required(ErrorMessage = "Phone number is required")]
        public required string PhoneNumber { get; set; } // Номер телефона

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; } // Электронная почта
        public bool? Courier { get; set; } // Является ли Курьером?
        public bool? Admin { get; set; } // Является ли Админом?
        public record Position(double Latitude, double Longitude);

        public string? Password { get; set; } // Хэш пароля
        public double? Raiting { get; set; }

        [JsonIgnore]
        public virtual List<Order> Orders { get; set; } = new(); // Заказы как клиент

        [JsonIgnore]
        public virtual List<Order> CourierOrders { get; set; } = new(); // Заказы как курьер

        [JsonIgnore]
        public virtual List<Cart>? Carts { get; set; }

        [JsonIgnore]
        public virtual ICollection<Favorite>? Favorites { get; set; } = new HashSet<Favorite>();

        [JsonIgnore]
        public List<Address>? ShippingAddresses { get; set; }

        public string? FcmToken { get; set; }

        public bool? IsOnline { get; set; }
    }
}
