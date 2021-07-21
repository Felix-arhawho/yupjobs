using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class Profile
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        public BusinessType BusinessType { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        //IF ORG
        public string OrgName { get; set; }

        public string TextBio { get; set; }


        /// <summary>
        /// name, years
        /// </summary>
        public List<DbTag> Skills { get; set; } = new List<DbTag>();
        public DateTime DoB { get; set; }

        public string ProfilePicture { get; set; }
        public List<string> ProfilePics { get; set; }
        public List<string> InfoPics { get; set; } = new List<string>();


        // ratings

    }

    public class ProfileRating
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string ProfileId { get; set; }

        public int Star1 { get; set; }
        public int Star2 { get; set; }
        public int Star3 { get; set; }
        public int Star4 { get; set; }
        public int Star5 { get; set; }
        public float Average { get
            {
                var cnt = Star1 + Star2 + Star3 + Star4 + Star5;
                float avg = ((Star1) + (Star2 * 2) + (Star3 * 3) + (Star4 * 4) + (Star5 * 5)) / (float)cnt;
                return avg;
            } }
    }
        


}
