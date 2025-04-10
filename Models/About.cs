using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ArzanGo.Models
{
    [Table("About")]
    public class About
    {
        public Guid AboutId { get; set; }

        [StringLength(3000)]
        [Required(ErrorMessage = "Пожалуйста введите название магазина")]
        [DisplayName("Название магазина")]
        public required string Name { get; set; }

        [StringLength(3000)]
        [Required(ErrorMessage = "Пожалуйста введите текст")]
        [DisplayName("Текст о нас")]
        public required string Description { get; set; }

        [StringLength(300)]
        [DisplayName("Фото")]
        public string? Resim { get; set; }

        // Время работы
        [StringLength(500)]
        [DisplayName("Часы работы")]
        public string? WorkingHours { get; set; }

        // Социальные сети
        [StringLength(200)]
        [DisplayName("Instagram")]
        public string? InstagramUrl { get; set; }

        [StringLength(200)]
        [DisplayName("Facebook")]
        public string? FacebookUrl { get; set; }

        [StringLength(200)]
        [DisplayName("Telegram")]
        public string? TelegramUrl { get; set; }

        [StringLength(200)]
        [DisplayName("WhatsApp")]
        public string? WhatsAppUrl { get; set; }

        [StringLength(200)]
        [DisplayName("YouTube")]
        public string? YouTubeUrl { get; set; }
    }
}
