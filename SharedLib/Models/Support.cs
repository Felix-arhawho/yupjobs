using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class SupportTicket
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string RequestTitle { get; set; }
        public string Message { get; set; }
        public DateTime DateSent { get; set; }
        public SupportCategory Category { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string StaffId { get; set; }
        public string StaffUsername { get; set; }
        public bool StaffAssigned { get; set; }
        public bool ClientShouldCheck { get; set; }
        public bool StaffShouldCheck { get; set; }

        public Dictionary<string, string> Meta = new Dictionary<string, string>();
    }

    public class TicketMessage
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string TicketId { get; set; }
        public bool IsStaff { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        
    }

    public enum SupportCategory
    {
        Account = 10,
        Billing = 15,
        Post = 20,
        Other = 100
    }

}
