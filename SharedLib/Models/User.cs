using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedLib.Lib;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.Models
{
    public class User
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public BusinessType BusinessType { get; set; }
        public string HashedPassword { get; set; }
        public DateTime DateCreated { get; set; }
        public bool Verified { get; set; }
        public string BackupEmail { get; set; }
        public CountryCode Country { get; set; }
        public Currency DefaultCurrency { get; set; }
        public UserStatus Status { get; set; }
        public string StripeCustomerId { get; set; }
        public bool ToSAccepted { get; set; } = true;

        public bool TwoFactorAuth { get; set; } = false;
    }

    public class Jwt
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ProfileId { get; set; }

        public string Username { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime Issued { get; set; }
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public async Task<bool> Valid()
        {
            try
            {
                //using var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "token", this.ToJson() } });
                //var resp = await Ez.HttpClient.PostAsync("https://mltapi1.azurewebsites.net/api/verify", content);

                using var resp = await Ez.GetHttpPostResponse("auth/verify");

                if (resp.IsSuccessStatusCode) return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

    }

    public class Otp
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }

        public string Code { get; set; }
        public OtpType Type { get; set; }
        public DateTime Issued { get; set; }
        public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        //public static Otp Generate(string userId, OtpType type)
        //{

        //    return new Otp();
        //}
    }

    public enum OtpType
    {
        Register,
        Login,
        PwdReset
    }

    public enum UserStatus
    {
        Active = 110,
        Inactive = 103,
        Banned = 5
    }
}
