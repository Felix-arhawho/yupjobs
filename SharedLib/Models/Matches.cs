using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class JobApply
    {
        [BsonId] [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ProfileId { get; set; }
        public string PostId { get; set; }
        public DateTime Date { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public decimal RequestedSalary { get; set; } = 150;
        public Currency Currency { get; set; }
        public short DaysToCompletion { get; set; }
        public bool Hidden { get; set; } = false;
        public List<string> Images { get; set; } = new List<string>();
    }

    public class SearchProposal
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string PostId { get; set; }
        public DateTime Date { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public decimal ProposedSalary { get; set; } = 100;
        public Currency Currency { get; set; }
        public bool Hidden { get; set; } = false;
    }
}
