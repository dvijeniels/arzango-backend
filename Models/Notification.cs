namespace ArzanGo.Models
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }  // Кому предназначено
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}
