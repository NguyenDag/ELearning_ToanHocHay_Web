using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    /// <summary>
    /// Gói của tôi · Lịch sử thanh toán · Hoàn tiền (P5 + P8).
    /// </summary>
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly SubscriptionApiService _subscriptions;
        private readonly PaymentApiService _payments;
        private readonly RefundApiService _refunds;

        public PaymentController(
            SubscriptionApiService subscriptions,
            PaymentApiService payments,
            RefundApiService refunds)
        {
            _subscriptions = subscriptions;
            _payments = payments;
            _refunds = refunds;
        }

        // GET /Payment/MySubscription
        public async Task<IActionResult> MySubscription()
        {
            var info = await _subscriptions.GetMySubscriptionAsync() ?? new SubscriptionInfoDto();
            return View(info);
        }

        // POST /Payment/CancelSubscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription(int subscriptionId)
        {
            var r = await _subscriptions.CancelAsync(subscriptionId);
            this.PushToastResult(r, "Đã huỷ gói. Bạn vẫn dùng được đến hết hạn hiện tại.");
            return RedirectToAction("MySubscription");
        }

        // GET /Payment/History?page=1
        public async Task<IActionResult> History(int page = 1)
        {
            var r = await _payments.GetMyPaymentsAsync(page);
            if (this.AuthRedirectOrNull(r) is { } redirect) return redirect;

            var data = r.IsSuccess && r.Data != null
                ? r.Data
                : new PagedResultDto<PaymentDto> { Page = page, PageSize = 20 };
            if (!r.IsSuccess) this.ShowToastError(r);
            return View(data);
        }

        // ---------------------------------------------------------------------
        // Hoàn tiền
        // ---------------------------------------------------------------------

        // GET /Payment/Refund?paymentId=123
        public async Task<IActionResult> Refund(int paymentId)
        {
            var p = await _payments.GetPaymentAsync(paymentId);
            if (!p.IsSuccess || p.Data == null)
            {
                this.PushToastError("Không tìm thấy giao dịch.");
                return RedirectToAction("History");
            }

            ViewBag.Payment = p.Data;
            return View(new CreateRefundRequestDto { PaymentId = paymentId, Amount = p.Data.Amount });
        }

        // POST /Payment/Refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(CreateRefundRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                var p = await _payments.GetPaymentAsync(model.PaymentId);
                ViewBag.Payment = p.Data;
                return View(model);
            }

            var r = await _refunds.CreateAsync(model);

            if (r.IsSuccess)
            {
                this.PushToastSuccess(
                    "Đã gửi yêu cầu hoàn tiền. Bộ phận tài chính sẽ xử lý trong vài ngày làm việc.");
                return RedirectToAction("MyRefunds");
            }

            // 400 điều kiện · 409 đã có yêu cầu / hết hạn mức · 429 quá nhanh → toast
            this.ShowToastError(r);
            var pay = await _payments.GetPaymentAsync(model.PaymentId);
            ViewBag.Payment = pay.Data;
            return View(model);
        }

        // GET /Payment/MyRefunds?page=1
        public async Task<IActionResult> MyRefunds(int page = 1)
        {
            var r = await _refunds.GetMineAsync(page);
            if (this.AuthRedirectOrNull(r) is { } redirect) return redirect;

            var data = r.IsSuccess && r.Data != null
                ? r.Data
                : new PagedResultDto<RefundRequestDto> { Page = page, PageSize = 20 };
            if (!r.IsSuccess) this.ShowToastError(r);
            return View(data);
        }

        // GET /Payment/RefundDetail/5
        public async Task<IActionResult> RefundDetail(int id)
        {
            var r = await _refunds.GetAsync(id);
            if (!r.IsSuccess || r.Data == null)
            {
                this.PushToastError(r);
                return RedirectToAction("MyRefunds");
            }
            return View(r.Data);
        }
    }
}
