using MongoDB.Bson.Serialization.Attributes;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerLib.Models
{
    public class UserMedia
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string FullUrl { get; set; }
        public string HttpFormat { get; set; }
        public MediaType Type { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime ExpiresOn { get; set; }
    }

    //public enum MediaType
    //{
    //    General = 0,
    //    PostImage = 1,
    //    AccountImage = 2
    //}
}
