using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = GenerateOrderNumber();
        public Guid UserId { get; set; } 

        [JsonIgnore]
        public virtual User? Users { get; set; }

        public Guid AddressId { get; set; } 

        [JsonIgnore]
        public virtual Address? Address { get; set; }

        [JsonIgnore]
        public virtual List<OrderItem>? OrderItems { get; set; } // Список товаров, заказанных в рамках этого заказа
        public decimal TotalAmount { get; set; } // Общая сумма заказа
        public DateTime OrderDate { get; set; } // Дата создания заказа

        [StringLength(20)]
        [DisplayName("Вид оплаты")]
        public string? BuyingType { get; set; }

        [StringLength(2000)]
        [DisplayName("Дополнительно к заказу")]
        public string? Comment { get; set; }
        public Status Status { get; set; } // Статус заказа (например, "В обработке", "Доставлен")

        private static string GenerateOrderNumber()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
    
    public enum Status
    {
        [Display(Name = "В обработке")]
        InProcessing,
        [Display(Name = "Принят курьером")]
        IsReceivedByCourier,
        [Display(Name = "В пути")]
        IsOnTheWay,
        [Display(Name = "Доставлен")]
        IsDelivered,
        [Display(Name = "Отменён")]
        Canceled
    }
}
