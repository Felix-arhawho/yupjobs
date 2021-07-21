using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class StoreItem
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ProfileId { get; set; }
        public string Username { get; set; }

        //item
        public string ItemName { get; set; }
        public decimal ItemCost { get; set; }
        public Currency Currency { get; set; }
        public DateTime DatePosted { get; set; }
    }

    public class StoreItemOrder
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public StoreItem Item { get; set; }
        public DateTime Initiated { get; set; }
        public DateTime? DateFulfilled { get; set; }
    
    }

    public enum OrderStatus
    {
        Waiting,
        Confirmed,
        Paid,
        Fulfilled,
        Contested
    }

    public enum ItemCategory
    {
        Photography,
        Language,
        Audio,

    }
}
