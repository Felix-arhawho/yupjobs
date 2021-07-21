//using MongoDB.Driver;
//using OtaffSupport.Data;
//using SharedLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace OtaffSupport.Lib
//{
//    public static class Db
//    {
//        private static string MongoStr =
//            "mongodb+srv://lract72:EViccw51o4I3VeS3@mnclus-fc6yk.gcp.mongodb.net/test?retryWrites=true&w=majority";

//        public static MongoClient Client = new MongoClient(MongoStr);
//        public static IMongoDatabase Database = Client.GetDatabase("AuTaffPoto");

//        public static IMongoCollection<StaffUser> StaffUsers = Database.GetCollection<StaffUser>("StaffUsers");

//        //accounts
//        public static IMongoCollection<User> UsersCollection = Database.GetCollection<User>("Users");
//        public static IMongoCollection<Jwt> JwtsCollection = Database.GetCollection<Jwt>("Jwts");
//        public static IMongoCollection<Profile> ProfilesCollection = Database.GetCollection<Profile>("Profiles");
//        public static IMongoCollection<UserSubscription> SubscriptionsCollection = Database.GetCollection<UserSubscription>("Subscriptions");
//        public static IMongoCollection<UserAddress> UserAddress = Database.GetCollection<UserAddress>("UserAddresses");
//        public static IMongoCollection<ProfileRating> ProfileRatings = Database.GetCollection<ProfileRating>("ProfileRatings");

//        //verifs
//        public static IMongoCollection<Otp> OtpCollection = Database.GetCollection<Otp>("Otps");

//        //money
//        public static IMongoCollection<UserWallet> UserWalletsCollection = Database.GetCollection<UserWallet>("UserWallets");
//        public static IMongoCollection<WalletTransaction> WalletTransactionsCollection = Database.GetCollection<WalletTransaction>("WalletTransactions");
//        public static IMongoCollection<WalletTransaction> WalletTransactionsArchives = Database.GetCollection<WalletTransaction>("WalletTransactionsArchives");

//        public static IMongoCollection<UserPaymentMethod> PaymentMethodsCollection = Database.GetCollection<UserPaymentMethod>("UserPaymentMethods");

//        public static IMongoCollection<JobPayment> JobPaymentsCollection = Database.GetCollection<JobPayment>("JobPayments");
//        public static IMongoCollection<JobPayment> JobPaymentsArchive = Database.GetCollection<JobPayment>("JobPaymentsArchive");
//        public static IMongoCollection<UserBill> UserBillsCollection = Database.GetCollection<UserBill>("UserBill");

//        public static IMongoCollection<BillActionData> BillActions = Database.GetCollection<BillActionData>("BillActions");
//        public static IMongoCollection<SubscriptionActionData> SubBillActions = Database.GetCollection<SubscriptionActionData>("BillActions");
//        public static IMongoCollection<WalletRechargeActionData> WalletBillActions = Database.GetCollection<WalletRechargeActionData>("BillActions");
//        public static IMongoCollection<JobPaymentActionData> JobPaymentActions = Database.GetCollection<JobPaymentActionData>("BillActions");


//        public static IMongoCollection<UserConnectAccount> ConnectAccounts = Database.GetCollection<UserConnectAccount>("ConnectAccounts");

//        //public static IMongoCollection<FeeData> FeesCollection = Database.GetCollection<FeeData>("FeeData");
//        public static IMongoCollection<PayoutTransactionAction> TransactionActions = Database.GetCollection<PayoutTransactionAction>("TransactionActions");
//        public static IMongoCollection<PayoutToBank> BankPayoutActions = Database.GetCollection<PayoutToBank>("BankPayoutActions");

//        //posts
//        public static IMongoCollection<JobPost> JobPostsCollection = Database.GetCollection<JobPost>("JobPosts");
//        public static IMongoCollection<JobSearchPost> JobSearchPostsCollection = Database.GetCollection<JobSearchPost>("JobSearchPosts");

//        public static IMongoCollection<DbTag> TagMetaCollection = Database.GetCollection<DbTag>("DbTags");
//        public static List<DbTag> TagMetasLocalCollection = new List<DbTag>();

//        //media
//        //public static IMongoCollection<UserMedia> UserMediaCollection = Database.GetCollection<UserMedia>("UserMedia");

//        //jobs
//        public static IMongoCollection<Job> OngoingJobsCollection = Database.GetCollection<Job>("OngoingJobs");
//        public static IMongoCollection<Job> ArchivedJobsCollection = Database.GetCollection<Job>("ArchivedJobs");

//        //matching
//        public static IMongoCollection<JobApply> JobAppliesCollection = Database.GetCollection<JobApply>("JobApplies");
//        public static IMongoCollection<SearchProposal> SearchProposalsCollection = Database.GetCollection<SearchProposal>("SearchProposals");

//        //messaging
//        public static IMongoCollection<Conversation> ConversationsCollection = Database.GetCollection<Conversation>("Conversations");
//        public static IMongoCollection<ChatMessage> MessagesCollection = Database.GetCollection<ChatMessage>("Messages");

//        //reviews
//        public static IMongoCollection<ProfileReview> ReviewsCollections = Database.GetCollection<ProfileReview>("JobReviews");

//        //support
//        public static IMongoCollection<Report> ReportsCollection = Database.GetCollection<Report>("Reports");
//        public static IMongoCollection<SupportTicket> SupportTickets = Database.GetCollection<SupportTicket>("SupportTicket");
//        public static IMongoCollection<TicketMessage> TicketMessages = Database.GetCollection<TicketMessage>("TicketMessages");

//        //notifs
//        public static IMongoCollection<Notification> NotificationsCollections = Database.GetCollection<Notification>("Notifications");

//    }
//}
