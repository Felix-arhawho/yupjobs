using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib.Models
{
    public class StaffUser
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        public DateTime LastLogin { get; set; }
        public short Rank { get; set; } = 2;
        public List<SupportType> Types { get; set; } = new List<SupportType>();
    }
    public enum SupportType
    {
        Accounts,
        Money
    }
    public class StaffAction
    {
        [BsonId][BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string StaffId { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, dynamic> Data { get; set; } = new Dictionary<string, dynamic>();
    }
}
