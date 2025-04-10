using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ArzanGo.Models
{
    public class Message
    {
        public Guid MessageId { get; set; }

        public Guid UserId { get; set; }
        public User? Users { get; set; }

        [StringLength(3000)]
        [DisplayName("Введите тему")]
        public string? Title { get; set; }

        [StringLength(3000)]
        [Required(ErrorMessage = "Пожалуйста введите текст")]
        [DisplayName("Текст")]
        public required string MessageText { get; set; }
    }
}
