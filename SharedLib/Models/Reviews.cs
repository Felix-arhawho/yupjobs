using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class ProfileReview
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string ReviewContent { get; set; }
        public string TargetProfileId { get; set; }
        public string JobId { get; set; }
        public ReviewRating Rating { get; set; }
        public ReviewType Type { get; set; }
        public bool Hidden { get; set; } = false;
        public DateTime DatePosted { get; set; }
    }

    public enum ReviewRating
    {
        Horrible = 1,
        Bad = 2,
        Unsatisfied = 3,
        Satisfied = 4,
        Perfect = 5
    }

    public enum ReviewType
    {
        Employer,
        Employee
    }
}
