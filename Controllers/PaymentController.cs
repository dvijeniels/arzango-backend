using ArzanGo.Models;
using ArzanGo.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArzanGo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // GET: api/payments/active
        [HttpGet("active")]
        public IActionResult GetActivePaymentMethods()
        {
            var methods = _paymentService.GetActivePaymentMethods();
            return Ok(methods);  // 200 OK + JSON
        }

        // GET: api/payments/all
        [HttpGet("all")]
        public IActionResult GetAllPaymentMethods()
        {
            var methods = _paymentService.GetAllPaymentMethods();
            return Ok(methods);
        }

        // GET: api/payments/{id}
        [HttpGet("{id}")]
        public IActionResult GetPaymentMethodById(Guid id)
        {
            try
            {
                var method = _paymentService.GetPaymentMethodById(id);
                return Ok(method);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);  // 404 Not Found
            }
        }

        // PUT: api/payments/{id}
        [HttpPut("{id}")]
        public IActionResult UpdatePaymentMethod(Guid id, [FromBody] PaymentSettings settings)
        {
            if (id != settings.PaymentSettingId)  // Проверка на соответствие ID
                return BadRequest("ID in URL and body do not match.");

            try
            {
                _paymentService.UpdatePaymentMethod(settings);
                return NoContent();  // 204 No Content (успешное обновление)
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);  // 404 Not Found
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);  // 500 Internal Server Error
            }
        }
    }
}
