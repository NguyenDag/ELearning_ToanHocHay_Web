using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs.Payment
{
    // ---- kết quả phân trang chung của backend ----
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 1;
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    // ---- enums (serialize dạng chuỗi) ----
    public enum PaymentStatusVm { Pending, Completed, Failed, Refunded, PartiallyRefunded }
    public enum PaymentMethodVm { CreditCard, BankTransfer, Momo, ZaloPay, VNPay }
    public enum SubscriptionStatusVm { Active, Expired, Cancelled, Pending }

    public enum RefundReasonCode
    {
        DuplicatePayment, Overpayment, ServiceNotDelivered,
        CustomerRequest, BillingError, Goodwill, Other
    }

    public enum RefundRequestStatus
    {
        PendingReview, PendingSecondApproval, Approved, Batched,
        Disbursed, Completed, Rejected, Cancelled, Failed
    }

    public enum RefundEventType
    {
        Created, Approved, SecondApproved, Rejected, Cancelled,
        AddedToBatch, RemovedFromBatch, BatchExported, MarkedDisbursed,
        Confirmed, MarkedFailed, Retried
    }

    // ---- payments ----
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethodVm PaymentMethod { get; set; }
        public PaymentStatusVm Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; }

        public bool CanRefund =>
            Status is PaymentStatusVm.Completed or PaymentStatusVm.PartiallyRefunded;
    }

    // ---- refunds ----
    public class CreateRefundRequestDto
    {
        [Required] public int PaymentId { get; set; }

        /// <summary>Bỏ trống = hoàn toàn bộ phần còn lại.</summary>
        public decimal? Amount { get; set; }

        [Required] public RefundReasonCode ReasonCode { get; set; }

        [MaxLength(500)] public string? ReasonNote { get; set; }

        [Required, RegularExpression(@"^\d{4,12}$", ErrorMessage = "Mã ngân hàng (napas) phải là 4–12 chữ số.")]
        public string BankBin { get; set; } = "";

        [Required, RegularExpression(@"^\d{6,20}$", ErrorMessage = "Số tài khoản phải là 6–20 chữ số.")]
        public string BankAccountNumber { get; set; } = "";

        [Required, MaxLength(120)]
        public string BankAccountHolderName { get; set; } = "";
    }

    public class RefundRequestDto
    {
        public int RefundRequestId { get; set; }
        public Guid PublicId { get; set; }
        public int PaymentId { get; set; }
        public RefundReasonCode ReasonCode { get; set; }
        public string? ReasonNote { get; set; }
        public decimal Amount { get; set; }
        public RefundRequestStatus Status { get; set; }
        public string BankBin { get; set; } = "";
        public string BankAccountNumberLast4 { get; set; } = "";
        public string BankAccountHolderName { get; set; } = "";
        public DateTime? ApprovedAt { get; set; }
        public string? BankTransactionRef { get; set; }
        public string? RejectionReason { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundEventDto
    {
        public RefundEventType EventType { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? ActorUserType { get; set; }
        public decimal? AmountSnapshot { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundRequestDetailDto : RefundRequestDto
    {
        public List<RefundEventDto> Events { get; set; } = new();
    }
}
