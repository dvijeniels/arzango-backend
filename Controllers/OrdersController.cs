using ArzanGo.Data;
using ArzanGo.Models;
using ArzanGo.Models.Requests;
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

        public OrdersController(AppDbContext context, WebSocketHandler webSocketHandler)
        {
            _context = context;
            _webSocketHandler = webSocketHandler;
        }

        // ✅ Получить все заказы
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.Include(o => o.Users).Include(o => o.Address).Include(o => o.PaymentSettings)
                                        .Include(o => o.OrderItems)
                                        .ToListAsync();
        }

        // ✅ Получить заказ по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            var order = await _context.Orders.Include(o => o.Users).Include(o => o.Address).Include(o => o.PaymentSettings)
                                             .Include(o => o.OrderItems)
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
                .Include(o => o.OrderItems)
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
                OrderDate = DateTime.UtcNow,
                Status = Status.InProcessing,
                PaymentMethodId = request.PaymentMethodId,
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

            // 4. Очищаем корзину (но не удаляем CartItems для истории)
            cart.CartItems.Clear();
            cart.TotalAmount = 0;

            // 5. Сохраняем изменения
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _webSocketHandler.SendNotificationToUserAsync(userId,
                $"Ваш заказ #{order.OrderNumber} создан! Статус: {order.Status}");

            await _webSocketHandler.BroadcastOrdersUpdateAsync();
            return Ok(order);
        }

        [HttpPost("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
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
                    item.Product.Stok += item.Quantity;
                }
            }

            order.Status = Status.Canceled;
            await _context.SaveChangesAsync();
            await _webSocketHandler.SendNotificationToUserAsync(order.UserId,
                $"Заказ #{order.OrderNumber} отменён.");
            await _webSocketHandler.BroadcastOrdersUpdateAsync();
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

            await _webSocketHandler.BroadcastOrdersUpdateAsync();
            return NoContent();
        }
    }
}
