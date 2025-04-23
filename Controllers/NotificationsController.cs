using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // Получить все уведомления пользователя
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Notification>>> GetUserNotifications(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        // Пометить уведомление как прочитанное
        [HttpPatch("{notificationId}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Создать новое уведомление (например, при изменении корзины)
        [HttpPost]
        public async Task<ActionResult<Notification>> CreateNotification(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUserNotifications), new { userId = notification.UserId }, notification);
        }
    }
}
