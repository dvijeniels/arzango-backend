using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = GenerateOrderNumber();

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [JsonPropertyName("user")]
        public virtual User? User { get; set; }

        [ForeignKey("Courier")]
        public Guid? CourierId { get; set; } // ID курьера, который взял заказ
        public DateTime? AssignedDate { get; set; } // Дата назначения курьера

        [JsonPropertyName("courier")]
        public virtual User? Courier { get; set; }

        public Guid AddressId { get; set; }

        [JsonPropertyName("address")]
        public virtual Address? Address { get; set; }

        [JsonPropertyName("orderItems")]
        public virtual List<OrderItem>? OrderItems { get; set; } // Список товаров, заказанных в рамках этого заказа
        public decimal TotalAmount { get; set; } // Общая сумма заказа
        public DateTime OrderDate { get; set; } // Дата создания заказа

        [DisplayName("Вид оплаты")]
        [ForeignKey("PaymentSettings")]
        public Guid PaymentSettingId { get; set; } // ID метода оплаты

        [JsonPropertyName("paymentSettings")]
        [DisplayName("Вид оплаты")]
        public virtual PaymentSettings? PaymentSettings { get; set; }

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
