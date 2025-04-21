using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Address
    {
        public Guid AddressId { get; set; } // Уникальный идентификатор адреса
        public City City { get; set; } // Город (перечисление)
        public required string Street { get; set; } // Улица
        public required string House { get; set; } // Дом
        public string? Additionally { get; set; } // Номер квартиры, Подъезд

        [RegularExpression(@"^\d{6}$", ErrorMessage = "Неверный формат почтового индекса")]
        public string? PostalCode { get; set; } // Почтовый индекс

        // Идентификатор пользователя, к которому привязан адрес
        public Guid UserId { get; set; }

        [JsonIgnore]
        public virtual User? User { get; set; }
    }
    public enum City
    {
        [Display(Name = "Жалал-Абад")]
        JalalAbad,
        [Display(Name = "Ош")]
        Osh,
        [Display(Name = "Бишкек")]
        Bishkek,
        [Display(Name = "Нарын")]
        Naryn,
        [Display(Name = "Талас")]
        Talas,
        [Display(Name = "Ысык-Куль")]
        YsykKul,
        [Display(Name = "Баткен")]
        Batken,
        [Display(Name = "Иной")]
        Other
    }
}
