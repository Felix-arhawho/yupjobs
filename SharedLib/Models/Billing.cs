using MongoDB.Bson.Serialization.Attributes;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class UserBill
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        //info
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }
        public string Description { get; set; }

        //meta
        public PaymentFramework Framework { get; set; } = PaymentFramework.Intents;
        public MethodType MethodType { get; set; }
        public DateTime DateIssued { get; set; }
        public string StripeIntentId { get; set; }
        public string StripeInvoiceId { get; set; }
        public string TransactionId { get; set; }
        public short Priority { get; set; } = 1;

        //payment info
        public decimal TotalAmountRequested { get; set; }
        /// <summary>
        /// Total amount that is given to the user
        /// </summary>
        public decimal TotalAmountAllocated { get; set; }
        public Currency Currency { get; set; }
        public decimal[] TotalFees { get; set; }

        //status
        public SPaymentStatus Status { get; set; } = SPaymentStatus.Pending;
        public bool Captured { get; set; }
        public bool Paid { get; set; }
        public DateTime? PaidOn { get; set; }
        public bool AutoCharged { get; set; }

        // delayed notif
        public DateTime NextVerif { get; set; }
        public TimeSpan VerifInterval { get; set; } = TimeSpan.FromDays(1);
        public DateTime LastVerif { get; set; }

        //overdue
        public bool DueNoticeSent { get; set; }
        public bool DueNoticeMailSent { get; set; }
        public DateTime? DueNoticeSentDate { get; set; }

        //failed
        public short FailCount { get; set; }
        public List<decimal> FailFees { get; set; } = new List<decimal>();
        public bool ToPayIfFailed { get; set; }

        //actions
        public bool ShouldAct { get; set; }
        public BillAction Action { get; set; }
        public string ActionId { get; set; }

        //for invoices
        public string InvoiceUrl { get; set; }
        public short VerifsCount { get; set; }
    }

    public enum PaymentFramework
    {
        Intents = 0,
        Invoice = 1,
        YupWallet = 100,
        Paypal = 2,
        Skrill = 3,
        Crypto1 = 4,
    }

    public enum BillAction
    {
        None = 0,
        Debt = 12,
        RechargeWallet = 90,
        Subscription = 120,
        AddJobPayment = 200,
        ShopItem = 230,
        Promote = 239,
    }

    public class BillActionData
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string BillId { get; set; }
        public string Description { get; set; }
        public BillAction ActionType { get; set; }
        public DateTime Issued { get; set; }
        public bool Executed { get; set; } = false;
        public DateTime ExecutedOn { get; set; }
    }

    public class JobPaymentActionData : BillActionData
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string EmployerId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string EmployeeId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string JobId { get; set; }

        public Dictionary<string, decimal> Payments { get; set; } = new Dictionary<string, decimal>();
        public bool AutoRelease { get; set; }
        public Currency Currency { get; set; }
    }

    public class SubscriptionActionData : BillActionData
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string SubId { get; set; }

        public SubscriptionType Type { get; set; }
        public short Months { get; set; }

    }
    public class WalletRechargeActionData : BillActionData
    {
        public string WalletId { get; set; }
        public Currency Currency { get; set; }
        public decimal Amount { get; set; }
    }

    public class IntervalPaymentsActionData : BillActionData
    {
        public string TargetId { get; set; }
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
    }

    public class JobPromoteActionData : BillActionData
    {
        public string PostId { get; set; }
        public bool IsSearch { get; set; }
    }

    public class StoreItemActionData : BillActionData
    {
        public StoreItem Item { get; set; }
    }

    public class ShopItemActionData : BillActionData
    {
        public YupShopItem ShopItem { get; set; }
    }

    public class UserAddress
    {
        //[BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        //public string Id { get; set; }
        
        //[BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        //public string UserId { get; set; }

        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public enum BusinessType
    {
        individual = 0,
        company = 1,
        non_profit = 2,
        government_entity = 3
    }

    public enum SPaymentStatus
    {
        Success = 100,
        MethodRequired = 90,
        Fail = 17,
        Pending = 22,

    }
}
