using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class Notification
    {
        public Notification()
        {
            Date = DateTime.UtcNow;
        }

        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Href { get; set; } = "/";
        public bool Clicked { get; set; }
        public bool Seen { get; set; }
        public DateTime Date { get; set; }
    }
}
