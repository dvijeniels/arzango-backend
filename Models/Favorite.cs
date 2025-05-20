using System.Text.Json.Serialization;

namespace ArzanGo.Models
{
    public class Favorite
    {
        public Guid FavoriteId { get; set; } // Уникальный идентификатор
        public Guid UserId { get; set; } // Пользователь, которому нравится товар

        [JsonIgnore]
        public virtual User? User { get; set; }

        public Guid ProductId { get; set; } // Товар, который добавлен в избранное
        public virtual Product? Product { get; set; }

        public DateTime DateAdded { get; set; }
    }

}
