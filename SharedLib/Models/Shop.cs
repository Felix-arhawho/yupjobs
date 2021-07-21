using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class YupShopItem
    {
        [BsonId] [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public YupShopItemType ItemType { get; set; }
        public DateTime DateBought { get; set; }
        public DateTime? DateUsed { get; set; }
        public bool Used { get; set; }
        public Dictionary<string, dynamic> ActionData { get; set; } = new Dictionary<string, dynamic>();

        public static Dictionary<YupShopItemType, decimal> ShopItems = new Dictionary<YupShopItemType, decimal>() 
        {
            {YupShopItemType.ApplyPromo, 1.49m },
            {YupShopItemType.JobPromo, 1.99m },
            {YupShopItemType.Nda, 4.99m },
        }; 
    }

    public enum YupShopItemType
    {
        JobPromo = 10,
        ApplyPromo = 20,
        CreditAdd = 30,
        Nda = 40,
        BuyPosts = 50,
    }
}
