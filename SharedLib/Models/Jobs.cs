using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class Job
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string ConversationId { get; set; }

        public string JobTitle { get; set; }
        public JobPost OriginalPost { get; set; }
        public JobSearchPost OriginalSearch { get; set; }

        public string EmployerId { get; set; }
        public string EmployeeId { get; set; }
        public bool IsPaidOnPlatform { get; set; }
        public decimal Payment { get; set; }
        public Currency Currency { get; set; }
        public JobStatus Status { get; set; }
        public bool Active { get; set; }

        //public decimal FeeAmount { get; set; }
        //public Currency GetCurrency { get; set; }
        public bool FeePaid { get; set; } = false;
        public DateTime StartedOn { get; set; }
        public DateTime CompletedOn { get; set; }

    }

    public enum JobStatus
    {
        Started = 10,
        OnHold = 20,
        Completed = 30,
        Cancelled = 40,
        Disputed = 50
    }

    public class JobPayment
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string EmployerId { get; set; }
        public string EmployeeId { get; set; }
        public string JobId { get; set; }
        
        public decimal TransferAmount { get; set; }
        public Currency Currency { get; set; }
        public string StripeIntentId { get; set; }
        public string TempWalletId { get; set; }
        public decimal ReceiverFee { get; set; }

        public bool Released { get; set; }
        public bool Paid { get; set; }

        //meta
        public DateTime DateCreated { get; set; }
        public DateTime? DateReleased { get; set; }
        public string PaymentTitle { get; set; }
        //public DateTime? MyProperty { get; set; }
    }

    public enum PaymentStatus
    {
        Hold,
        Released,
        Cancelled,
        Blocked
    }
}
