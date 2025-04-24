using ArzanGo.Data;
using ArzanGo.Models;
using Microsoft.AspNetCore.Mvc;

namespace ArzanGo.Services
{

    //private readonly IPaymentService _paymentService;

    //public OrnekMethod(IPaymentService paymentService)
    //{
    //    _paymentService = paymentService;
    //}

    //public IActionResult OrnekIndex()
    //{
    //    var methods = _paymentService.GetAllPaymentMethods();
    //    return View(methods);
    //}

    public interface IPaymentService
    {
        IEnumerable<PaymentSettings> GetActivePaymentMethods();
        IEnumerable<PaymentSettings> GetAllPaymentMethods();
        PaymentSettings GetPaymentMethodById(Guid id);
        void UpdatePaymentMethod(PaymentSettings settings);
    }
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<PaymentSettings> GetActivePaymentMethods()
        {
            return _context.PaymentSettings
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToList();
        }

        public IEnumerable<PaymentSettings> GetAllPaymentMethods()
        {
            return _context.PaymentSettings
                .OrderBy(x => x.DisplayOrder)
                .ToList();
        }

        public PaymentSettings GetPaymentMethodById(Guid id)
        {
            var paymentSetting = _context.PaymentSettings.Find(id);
            if (paymentSetting == null)
                throw new KeyNotFoundException($"Payment method with ID {id} was not found.");

            return paymentSetting;
        }


        public void UpdatePaymentMethod(PaymentSettings settings)
        {
            _context.PaymentSettings.Update(settings);
            _context.SaveChanges();
        }
    }
}
