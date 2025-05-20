using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Requests;
using ArzanGo.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
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
        private readonly FirebaseNotificationService _firebaseService;
        private readonly IKyrgyzstanTimeService _timeService;

        public OrdersController(AppDbContext context, WebSocketHandler webSocketHandler, FirebaseNotificationService firebaseService, IKyrgyzstanTimeService timeService)
        {
            _context = context;
            _webSocketHandler = webSocketHandler;
            _firebaseService = firebaseService;
            _timeService = timeService;
        }

        // ✅ Курьер берет заказ
        [HttpPost("{orderId}/assign-courier/{courierId}")]
        public async Task<IActionResult> AssignCourierToOrder(Guid orderId, Guid courierId)
        {
            // 1. Проверяем существование заказа
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound("Order not found");

            // 2. Проверяем статус заказа
            if (order.Status != Status.InProcessing && order.Status != Status.Canceled)
                return BadRequest($"Order can only be assigned when in '{Status.InProcessing}' or '{Status.Canceled}' status. Current status: '{order.Status}'");

            // 3. Проверяем курьера
            var courier = await _context.Users.FindAsync(courierId);
            if (courier == null || courier.Courier != true)
                return BadRequest("Invalid courier");

            // 4. Проверяем активные заказы курьера
            //var hasActiveOrders = await _context.Orders
            //    .AnyAsync(o => o.CourierId == courierId &&
            //                  (o.Status == Status.IsReceivedByCourier || o.Status == Status.InProcessing || o.Status == Status.IsOnTheWay));

            //if (hasActiveOrders)
            //    return BadRequest("Courier already has active orders");

            // 5. Обновляем заказ
            order.CourierId = courierId;
            order.Status = Status.IsReceivedByCourier;
            // Получаем текущее время в UTC
            DateTime utcNow = DateTime.UtcNow;

            // Конвертируем в часовой пояс Кыргызстана
            DateTime kgTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcNow, "Central Asia Standard Time");

            order.AssignedDate = kgTime;

            // 6. Сохраняем изменения
            await _context.SaveChangesAsync();

            // 7. Функция для безопасной отправки уведомлений
            async Task SafeSendNotification(string token, string title, string body, Dictionary<string, string> data, string recipientType, Guid recipientId)
            {
                if (string.IsNullOrWhiteSpace(token))
                    return;

                try
                {
                    // Определяем проект Firebase по типу получателя
                    var projectType = recipientType.ToLower() switch
                    {
                        "user" => FirebaseProjectType.Users,
                        "courier" => FirebaseProjectType.Couriers,
                        _ => throw new ArgumentException($"Unknown recipient type: {recipientType}")
                    };

                    await _firebaseService.SendNotificationToUserAsync(token, title, body, data, projectType);
                }
                catch (FirebaseMessagingException ex) when (ex.ErrorCode == ErrorCode.NotFound)
                {
                    // Удаляем невалидный токен
                    if (recipientType == "user" && order.User != null)
                        order.User.FcmToken = null;
                    else if (recipientType == "courier")
                        courier.FcmToken = null;

                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {

                }
            }


            // Пользователю
            if (!string.IsNullOrEmpty(order.User?.FcmToken))
            {
                await SafeSendNotification(
                    order.User.FcmToken,
                    "Курьер назначен",
                    $"Курьер {courier.FirstName} {courier.LastName} принял ваш заказ #{order.OrderNumber}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "courier_assigned" },
                { "courierName", $"{courier.FirstName} {courier.LastName}" }
                    },
                    "user",
                    order.UserId);
            }

            // Курьеру
            if (!string.IsNullOrEmpty(courier.FcmToken))
            {
                await SafeSendNotification(
                    courier.FcmToken,
                    "Новый заказ",
                    $"Вы приняли заказ #{order.OrderNumber}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "order_assigned" }
                    },
                    "courier",
                    courierId);
            }

            // Отправка WebSocket уведомлений
            try
            {
                await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                    $"Курьер {courier.FirstName} {courier.LastName} принял ваш заказ #{order.OrderNumber}");

                await _webSocketHandler.SendNotificationToUserAsync(courierId,
                    $"Вы приняли заказ #{order.OrderNumber}");

                await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
            }
            catch
            {
                // Игнорируем ошибки WebSocket
            }

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
            return await _context.Orders.Include(o => o.User).Include(o => o.Address).Include(o => o.PaymentSettings).Include(o => o.Courier)
                                        .Include(o => o.OrderItems!).ThenInclude(o => o.Product).ThenInclude(o => o!.ProductPhotos)
                                        .ToListAsync();
        }

        // ✅ Получить заказ по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            var order = await _context.Orders.Include(o => o.User).Include(o => o.Address).Include(o => o.PaymentSettings).Include(o => o.Courier)
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
                .Include(o => o.User)
                .Include(o => o.PaymentSettings)
                .Include(o => o.Courier)
                .Include(o => o.Address)
                .Include(o => o.OrderItems!).ThenInclude(o => o.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpGet("courier/{courierId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCourier(Guid courierId)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentSettings)
                .Include(o => o.Courier)
                .Include(o => o.Address)
                .Include(o => o.OrderItems!).ThenInclude(o => o.Product)
                .Where(o => o.CourierId == courierId)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpGet("total/{courierId}")]
        public async Task<ActionResult<int>> GetTotalOrdersByCourier(Guid courierId)
        {
            var today = _timeService.Now.Date; // текущая дата без времени (UTC)
            var tomorrow = today.AddDays(1);

            var count = await _context.Orders
                .Where(o => o.Status == Status.IsDelivered)
                .Where(o => o.CourierId == courierId)
                .Where(o => o.OrderDate >= today && o.OrderDate < tomorrow)
                .CountAsync();

            return Ok(count);
        }
        [HttpGet("get-active-order/{courierId}")]
        public async Task<ActionResult<int>> GetActiveOrdersByCourier(Guid courierId)
        {
            var count = await _context.Orders
                .Where(o => o.Status == Status.IsReceivedByCourier || o.Status==Status.IsOnTheWay)
                .Where(o => o.CourierId == courierId)
                .ToListAsync();
            return Ok(count);
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

            try
            {
                // 2. Создаем заказ
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = _timeService.Now,
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
                var availableCouriers = await _context.Users
                    .Where(u => u.Courier == true && !string.IsNullOrEmpty(u.FcmToken))
                    .ToListAsync();


                foreach (var courier in availableCouriers)
                {
                    await _firebaseService.SendNotificationToUserAsync(
                        courier.FcmToken!,
                        "Новый заказ",
                        $"Доступен новый заказ #{order.OrderNumber}",
                        new Dictionary<string, string>
                        {
                            { "orderId", order.OrderId.ToString() },
                            { "type", "new_order" }
                        },
                        FirebaseProjectType.Couriers);
                }


                await _webSocketHandler.SendNotificationToUserAsync(userId,
                    $"Ваш заказ #{order.OrderNumber} создан! Статус: {order.Status}");

                await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPatch("{orderId}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            // 1. Получаем заказ с необходимыми включениями
            var order = await _context.Orders
                .Include(o => o.User)
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
            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);
            if (!string.IsNullOrEmpty(order.User?.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    order.User.FcmToken,
                    $"Статус заказа обновлен",
                    $"Заказ #{order.OrderNumber}: {order.Status}",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "order_status_update" },
                { "newStatus", order.Status.ToString() }
                    });
            }

            if (order.CourierId.HasValue)
            {
                var courier = await _context.Users.FindAsync(order.CourierId.Value);
                if (!string.IsNullOrEmpty(courier?.FcmToken))
                {
                    await _firebaseService.SendNotificationToUserAsync(
                        courier.FcmToken,
                        "Изменение статуса заказа",
                        $"Заказ #{order.OrderNumber} теперь имеет статус: {order.Status}",
                        new Dictionary<string, string>
                        {
                            { "orderId", order.OrderId.ToString() },
                            { "type", "order_status_changed" },
                            { "newStatus", order.Status.ToString() }
                        },
                        FirebaseProjectType.Couriers);
                }
            }


            return Ok(order);
        }

        public class UpdateOrderStatusRequest
        {
            public Status NewStatus { get; set; }
            public string? Comment { get; set; }
        }

        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId, string? comment)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(ci => ci.Product)
                .Include(o => o.PaymentSettings)
                .Include(o => o.User)
                .Include(o => o.Address)
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

            // WebSocket уведомление
            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Заказ #{order.OrderNumber} отменён.");
            await _webSocketHandler.SendOrderUpdateAsync(order.OrderId);

            // Push уведомление пользователю
            if (!string.IsNullOrEmpty(order.User?.FcmToken))
            {
                await _firebaseService.SendNotificationToUserAsync(
                    order.User.FcmToken,
                    "Заказ отменён",
                    $"Ваш заказ #{order.OrderNumber} был отменён",
                    new Dictionary<string, string>
                    {
                { "orderId", order.OrderId.ToString() },
                { "type", "order_canceled" }
                    },
                    FirebaseProjectType.Users);
            }

            // (опционально) Push курьеру, если он был назначен
            if (order.CourierId.HasValue)
            {
                var courier = await _context.Users.FindAsync(order.CourierId.Value);
                if (!string.IsNullOrEmpty(courier?.FcmToken))
                {
                    await _firebaseService.SendNotificationToUserAsync(
                        courier.FcmToken,
                        "Заказ отменён",
                        $"Заказ #{order.OrderNumber}, который вы приняли, был отменён пользователем.",
                        new Dictionary<string, string>
                        {
                    { "orderId", order.OrderId.ToString() },
                    { "type", "order_canceled" }
                        },
                        FirebaseProjectType.Couriers);
                }
            }

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
