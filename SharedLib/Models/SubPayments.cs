using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class IntervalPaymentOrder
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string JobId { get; set; }
        public string EmployerId { get; set; }
        public string EmployerUsername { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeUsername { get; set; }
        public string Title { get; set; }

        //money
        public string PaymentMethodId { get; set; }
        public decimal Payment { get; set; }
        public TimeSpan PaymentInterval { get; set; }
        public Currency Currency { get; set; }

        //meta
        public DateTime StartedOn { get; set; }
        public DateTime? EndedOn { get; set; }
        public bool Active { get; set; }
        public DateTime LastPayment { get; set; }
        public DateTime NextPayment { get; set; }

        public bool PaymentFailed { get; set; }
    }

    public class IntervalPayment
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string JobId { get; set; }
        public string EmployerId { get; set; }
        public string EmployeeId { get; set; }
        public DateTime CreatedOn { get; set; }
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public bool Paid { get; set; }
        public DateTime PaidOn { get; set; }
        public string MethodId { get; set; }
    }
}
