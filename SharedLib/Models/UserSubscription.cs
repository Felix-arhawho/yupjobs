using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class UserSubscription
    {
        [BsonId] [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        //public bool IsValid { get; set; }
        public DateTime ValidUntil { get; set; } = DateTime.MinValue;
        public TimeSpan GracePeriod { get; set; } = TimeSpan.FromDays(5);
        public bool Renew { get; set; }
        public Currency Currency { get; set; }
        public string PaymentMethodId { get; set; }
        public SubscriptionType Type { get; set; }
        public SubscriptionType NextType { get; set; }
        public SubscriptionLimits Limits { get => SubscriptionsMeta.Limits[Type]; }
        public bool StillFresh { get; set; } = true;

        //billing
        public bool OngoingRecharge { get; set; }
    }
    //public class UserBill
    //{
    //    [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    //    public string Id { get; set; }
    //    public string UserId { get; set; }
    //    public string Description { get; set; }
    //    public DateTime Date { get; set; }
    //}

    public static class SubscriptionsMeta
    {
        public static Dictionary<SubscriptionType, decimal> SubscriptionCosts = new Dictionary<SubscriptionType, decimal> 
        {
            {SubscriptionType.Free, 0},
            {SubscriptionType.Personal, 5.99m},
            {SubscriptionType.Pro, 12.99m},
            {SubscriptionType.Business, 19.99m},
        };

        public static readonly Dictionary<SubscriptionType, SubscriptionLimits> Limits = new Dictionary<SubscriptionType, SubscriptionLimits>
        {
            {SubscriptionType.Free, new SubscriptionLimits() },
            {SubscriptionType.Personal, new SubscriptionLimits()
            {
                JobPostLimit = 5,
                JobSearchPostLimit = 3,
                WeeklyApplyLimit = 25,
                PerJobPostAcceptLimit = 3,
                JobPostApplicationLimit = 12,
                WeeklyJobSearchProposalsLimit = 7
            } },
            {SubscriptionType.Pro, new SubscriptionLimits()
            {
                JobPostLimit = 8,
                JobSearchPostLimit = 6,
                WeeklyApplyLimit = 35,
                PerJobPostAcceptLimit = 6,
                JobPostApplicationLimit = 18,
                WeeklyJobSearchProposalsLimit = 12,
                PostTagCount = 10
            } },
            {SubscriptionType.Business, new SubscriptionLimits()
            {
                JobPostLimit = 12,
                JobSearchPostLimit = 14,
                WeeklyApplyLimit = 80,
                PerJobPostAcceptLimit = 12,
                JobPostApplicationLimit = 30,
                WeeklyJobSearchProposalsLimit = 20,
                PostTagCount = 15
            } }
        };
    }
    public class SubscriptionLimits
    {
        //These are the defaults for the free subscription

        //FOR EMPLOYERS
        /// <summary>
        /// number of active job posts allowed
        /// </summary>
        public short JobPostLimit = 2;
        /// <summary>
        /// number of applications a job post can have
        /// </summary>
        public short JobPostApplicationLimit = 8;
        /// <summary>
        /// number of candidates that you can accept
        /// </summary>
        public short PerJobPostAcceptLimit = 1;
        /// <summary>
        /// number of job searches you can propose to
        /// </summary>
        public short WeeklyJobSearchProposalsLimit = 4;

        //FOR EMPLOYEES
        /// <summary>
        /// number of active job searches that you can post
        /// </summary>
        public short JobSearchPostLimit = 1;
        /// <summary>
        /// number of applications to job posts for the last week
        /// </summary>
        public short WeeklyApplyLimit = 12;

        /// <summary>
        /// Tag count limit
        /// </summary>
        public short PostTagCount = 6;

        /// <summary>
        /// Post image limit
        /// </summary>
        public short PostImageCount = 2;

        //BONUSES
        public short ExtraPosts = 0;
        public short ExtraApplies = 0;
    }

    public enum SubscriptionType
    {
        Free,
        Personal,
        Pro,
        Business
    }
}
