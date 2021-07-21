using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class UserPaymentMethod
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }

        public MethodType Type { get; set; }
        public string MethodId { get; set; }
        public bool Default { get; set; }

        /// <summary>
        /// Last 4 digits or bank name
        /// </summary>
        public string MetaName { get; set; }
        public string OwnerName { get; set; }
        //public string  { get; set; }

    }

    public enum MethodType
    {
        CreditCard = 10,
        SepaDirect = 250,
        BecsDirect = 251,
        Sofort = 303,
        Przelewy24 = 24,
        StripeInvoice = 203,
        PayPal = 905,
        YupWallet = 1001,
        //AchDirect = 255
    }
}
