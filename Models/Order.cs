using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ArzanGo.Models
{
    public class Order
    {
        public Guid OrderId { get; set; } // Уникальный идентификатор заказа
        public Guid UserId { get; set; } // Идентификатор пользователя, сделавшего заказ
        public virtual User? Users { get; set; } // Навигационное свойство для пользователя

        public Guid CourierId { get; set; } // Идентификатор курьера
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
        IsDelivered
    }
}
