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
        [Required(ErrorMessage = "Пожалуйста введите текст")]
        [DisplayName("Текст о нас")]
        public string Description { get; set; }

        [StringLength(300)]
        [DisplayName("Фото")]
        public string Resim { get; set; }
    }
}
