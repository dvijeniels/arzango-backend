using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class PaymentSettings
    {
        [Key]
        public Guid PaymentSettingId { get; set; }

        [DisplayName("Название метода оплаты")]
        public required string Name { get; set; }

        [DisplayName("Активен")]
        public bool IsActive { get; set; }

        [DisplayName("Порядок отображения")]
        public int? DisplayOrder { get; set; }

        [JsonIgnore]
        public virtual ICollection<Order>? Orders { get; set; }
    }
}
