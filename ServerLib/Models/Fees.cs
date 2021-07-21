using MongoDB.Bson.Serialization.Attributes;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib.Models
{
    public class FeeData
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string MetaId { get; set; }
        public FeeType Type { get; set; }
        public decimal Units { get; set; }
        public Currency Currency { get; set; }
        public DateTime Date { get; set; }
    }

    public enum FeeType
    {
        Payment = 0,
        Transaction = 1
    }
}
