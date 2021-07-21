using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib.Models
{
    public class ChargeDispute
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string DisputeId { get; set; }
        public Dispute DisputeObj { get; set; }
    }
}
