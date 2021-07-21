using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharedLib.Models
{
    public class UserConnectAccount
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string StripeCustomerId { get; set; }
        public string ConnectId { get; set; }

        public decimal RegistrationPaidAmount { get; set; } = 0;
        public Currency RegistrationCurrency { get; set; }
    }

    public class ConnectAccountDetails
    {
        [NotNull] public string FirstName { get; set; }
        [NotNull] public string LastName { get; set; }

        /// <summary>
        /// Company name, optional
        /// </summary>
        public string OrgName { get; set; }
        public UserAddress Address { get; set; }
    }

}
