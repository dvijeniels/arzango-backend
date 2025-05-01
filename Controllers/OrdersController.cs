using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Requests;
using ArzanGo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArzanGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly IPaymentService _paymentService;
        private readonly FirebaseNotificationService _firebaseService;

        public OrdersController(AppDbContext context, WebSocketHandler webSocketHandler, IPaymentService paymentService, FirebaseNotificationService firebaseService)
        {
            _context = context;
            _webSocketHandler = webSocketHandler;
            _paymentService = paymentService;
            _firebaseService = firebaseService;
        }

        // ✅ Курьер берет заказ
        [HttpPost("{orderId}/assign-courier/{courierId}")]
        [Authorize(Roles = "Courier,Admin")] // Только курьеры и админы могут вызывать этот метод
        public async Task<IActionResult> AssignCourierToOrder(Guid orderId, Guid courierId)
        {
            // 1. Проверяем существование заказа
            var order = await _context.Orders
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound("Order not found");

            // 2. Проверяем, что заказ в правильном статусе
            if (order.Status != Status.InProcessing && order.Status != Status.InProcessing)
                return BadRequest("Order cannot be assigned in current status");

            // 3. Проверяем, что курьер существует и действительно является курьером
            var courier = await _context.Users.FindAsync(courierId);
            if (courier == null || courier.Courier != true)
                return BadRequest("Invalid courier");

            // 4. Проверяем, что курьер не имеет других активных заказов
            var hasActiveOrders = await _context.Orders
                .AnyAsync(o => o.CourierId == courierId &&
                              (o.Status == Status.IsReceivedByCourier || o.Status == Status.InProcessing || o.Status == Status.IsOnTheWay));

            if (hasActiveOrders)
                return BadRequest("Courier already has active orders");

            // 5. Обновляем заказ
            order.CourierId = courierId;
            order.Status = Status.IsReceivedByCourier;
            order.AssignedDate = DateTime.Now;

            // 6. Сохраняем изменения
            await _context.SaveChangesAsync();

            // 7. Отправляем уведомления
            // Пользователю
            if (!string.IsNullOrEmpty(order.Users?.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    order.Users.FcmToken,
                    "Курьер назначен",
                    $"Курьер {courier.FirstName} {courier.LastName} принял ваш заказ #{order.OrderNumber}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "courier_assigned" },
                { "courierName", $"{courier.FirstName} {courier.LastName}" }
                    });
            }

            // Курьеру
            if (!string.IsNullOrEmpty(courier.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    courier.FcmToken,
                    "Новый заказ",
                    $"Вы приняли заказ #{order.OrderNumber}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "order_assigned" }
                    });
            }

            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Курьер {courier.FirstName} {courier.LastName} принял ваш заказ #{order.OrderNumber}");

            await _webSocketHandler.SendNotificationToUserAsync(courierId,
                $"Вы приняли заказ #{order.OrderNumber}");

            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);

            return Ok(new
            {
                OrderId = order.OrderId,
                Status = order.Status,
                Courier = new
                {
                    courier.UserId,
                    courier.FirstName,
                    courier.LastName,
                    courier.PhoneNumber,
                    courier.Email,
                    courier.Raiting
                }
            });
        }

        // ✅ Получить все заказы
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.Include(o => o.Users).Include(o => o.Address).Include(o => o.PaymentSettings)
                                        .Include(o => o.OrderItems!).ThenInclude(o=>o.Product)
                                        .ToListAsync();
        }

        // ✅ Получить заказ по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            var order = await _context.Orders.Include(o => o.Users).Include(o => o.Address).Include(o => o.PaymentSettings)
                                             .Include(o => o.OrderItems!).ThenInclude(o => o.Product)
                                             .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return order;
        }
        // ✅ Получить все заказы пользователя
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByUser(Guid userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Users)
                .Include(o => o.PaymentSettings)
                .Include(o => o.Address)
                .Include(o => o.OrderItems!).ThenInclude(o => o.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPost("user/{userId}/create-order")]
        public async Task<ActionResult<Order>> CreateOrder(Guid userId, [FromBody] OrderRequest request)
        {
            // 1. Получаем корзину пользователя
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                return BadRequest("Cart is empty");

            // 2. Создаем заказ
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = Status.InProcessing,
                PaymentSettingId = request.PaymentSettingId,
                Comment = request.Comment,
                AddressId = request.AddressId,
                TotalAmount = cart.TotalAmount
            };

            // 3. Переносим товары из корзины в заказ
            order.OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Price = ci.Price,
                Quantity = ci.Quantity
            }).ToList();

            foreach (var item in order.OrderItems!)
            {
                if (item?.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                }
            }
            // 4. Очищаем корзину (но не удаляем CartItems для истории)
            cart.CartItems.Clear();
            cart.TotalAmount = 0;

            // 5. Сохраняем изменения
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            if (!string.IsNullOrEmpty(user?.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    user.FcmToken,
                    "Новый заказ создан",
                    $"Ваш заказ #{order.OrderNumber} успешно создан",
                    new Dictionary<string, string>
                    {
                    { "orderId", order.OrderId.ToString() },
                    { "type", "order_created" }
                    });
            }

            await _webSocketHandler.SendNotificationToUserAsync(userId,
                $"Ваш заказ #{order.OrderNumber} создан! Статус: {order.Status}");

            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
            return Ok(order);
        }

        [HttpPatch("{orderId}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            // 1. Получаем заказ с необходимыми включениями
            var order = await _context.Orders
                .Include(o => o.Users)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound("Order not found");

            // 3. Сохраняем предыдущий статус для логов/уведомлений
            var previousStatus = order.Status;
            var previousComment = order.Comment;
            order.Status = request.NewStatus;
            order.Comment = request.Comment;

            // 5. Сохраняем изменения
            await _context.SaveChangesAsync();

            // 6. Отправляем уведомления
            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Статус заказа #{order.OrderNumber} был изменён {order.Status}.");

            if (!string.IsNullOrEmpty(order.Users?.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    order.Users.FcmToken,
                    $"Статус заказа обновлен",
                    $"Заказ #{order.OrderNumber}: {order.Status}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "order_status_update" },
                { "newStatus", order.Status.ToString() }
                    });
            }

            return Ok(order);
        }

        public class UpdateOrderStatusRequest
        {
            public Status NewStatus { get; set; }
            public string? Comment { get; set; }
        }

        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId,string? comment)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                .ThenInclude(ci => ci.Product).Include(o => o.PaymentSettings).Include(o => o.Users).Include(o => o.Address)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();
            if (order.Status != Status.InProcessing)
                return BadRequest("Order cannot be canceled");

            // Возвращаем товары на склад
            foreach (var item in order.OrderItems!)
            {
                if (item?.Product != null)
                {
                    item.Product.Stock += item.Quantity;
                }
            }

            order.Status = Status.Canceled;
            order.Comment = comment;
            await _context.SaveChangesAsync();
            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Заказ #{order.OrderNumber} отменён.");
            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
            return Ok();
        }

        // ✅ Удалить заказ
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Заказ #{order.OrderNumber} удалён администратором.");

            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
            return NoContent();
        }
    }
}
