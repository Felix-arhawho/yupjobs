using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class Dispute
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public List<ValueTuple<string, string>> UserIds { get; set; } = new List<ValueTuple<string,string>>();
        public List<string> StaffIds { get; set; } = new List<string>();
        public bool StaffAssigned { get; set; }
        public DateTime DateStarted { get; set; }
        public string JobId { get; set; }
        public string ConversationId { get; set; }
    }
}
