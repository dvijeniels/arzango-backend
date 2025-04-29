using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArzanGo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FirebaseNotificationService _firebaseService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(AppDbContext context, FirebaseNotificationService firebaseService,ILogger<NotificationsController> logger)
        {
            _context = context;
            _firebaseService = firebaseService;
            _logger = logger;
        }

        [HttpPost("update-token")]
        public async Task<IActionResult> UpdateToken([FromBody] UpdateTokenRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound("User not found");

            user.FcmToken = request.Token;
            await _context.SaveChangesAsync();

            return Ok();
        }

        public class UpdateTokenRequest
        {
            public Guid UserId { get; set; }
            public required string Token { get; set; }
        }

        [HttpPost("send-to-user")]
        public async Task<IActionResult> SendToUser([FromBody] SendToUserRequest request)
        {
            try
            {
                await _firebaseService.SendNotificationToUserAsync(
                    request.DeviceToken,
                    request.Title,
                    request.Body,
                    request.Data);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, new { Error = "Failed to send notification" });
            }
        }

        [HttpPost("send-to-topic")]
        public async Task<IActionResult> SendToTopic([FromBody] SendToTopicRequest request)
        {
            try
            {
                await _firebaseService.SendNotificationToTopicAsync(
                    request.Topic,
                    request.Title,
                    request.Body,
                    request.Data);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to topic");
                return StatusCode(500, new { Error = "Failed to send notification" });
            }
        }

        public class SendToUserRequest
        {
            public required string DeviceToken { get; set; }
            public required string Title { get; set; }
            public required string Body { get; set; }
            public Dictionary<string, string>? Data { get; set; }
        }

        public class SendToTopicRequest
        {
            public required string Topic { get; set; }
            public required string Title { get; set; }
            public required string Body { get; set; }
            public Dictionary<string, string>? Data { get; set; }
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
