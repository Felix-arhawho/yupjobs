using MongoDB.Driver;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Workers
{
    public static partial class BillWorker
    {
        public static void SetPaid(this UserBill bill, ref Invoice invoice, IClientSessionHandle s)
        {
            var upd = new UpdateDefinitionBuilder<UserBill>()
                .Set(x => x.Paid, true)
                .Set(x => x.PaidOn, DateTime.UtcNow)
                .Set(x => x.Status, SPaymentStatus.Success)
                .Set(x => x.TotalAmountAllocated, invoice.AmountPaid.GetMoney(bill.Currency))
                .Set(x => x.ShouldAct, bill.Action is BillAction.None ? false : true)
                .Set(x => x.PaidOn, DateTime.UtcNow);

            Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
        }

        public static void SetPaid(this UserBill bill, ref PaymentIntent intent, IClientSessionHandle s)
        {
            var upd = new UpdateDefinitionBuilder<UserBill>()
                .Set(x => x.Paid, true)
                .Set(x => x.PaidOn, DateTime.UtcNow)
                .Set(x => x.Status, SPaymentStatus.Success)
                .Set(x => x.TotalAmountAllocated, intent.AmountReceived.GetMoney(bill.Currency))
                .Set(x => x.ShouldAct, bill.Action is BillAction.None ? false : true)
                .Set(x => x.PaidOn, DateTime.UtcNow);

            Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
        }

        public static void Action(this JobPaymentActionData action, UserBill bill, IClientSessionHandle s)
        {
            Console.WriteLine("Doing job payment action for job " + action.JobId);
            var job = Db.OngoingJobsCollection.FirstAsync(x => x.Id == action.JobId);
            UserWallet wallet;
            if (action.AutoRelease)
            {
                var usr = Db.UsersCollection.First(x => x.Id == action.EmployeeId);
                //wallet = WalletCtl.GetUserWallet(action.EmployeeId, action.Currency).Result;

                var pmt = new JobPayment()
                {
                    DateCreated = DateTime.UtcNow,
                    StripeIntentId = bill.StripeIntentId,
                    Currency = action.Currency,
                    Released = true,
                    EmployeeId = action.EmployeeId,
                    EmployerId = action.EmployerId,
                    JobId = action.JobId,
                    Paid = true,
                    ReceiverFee = 0,
                    PaymentTitle = $"Payment of {action.Payments.Values.Sum()} {Enum.GetName(typeof(Currency), action.Currency)} for {usr.Username}",
                    DateReleased = DateTime.UtcNow,
                    TransferAmount = action.Payments.Values.Sum()
                };
                
                var t = Db.JobPaymentsCollection.InsertOneAsync(s, pmt);

                var amt = action.Payments.Values.Sum();
                var totr = amt - (decimal)((float)amt * 0.043f);

                WalletCtl.CreateTransferFromPlatform(usr, action.Currency, totr, s: s).Wait();
                
                //var upd0 = new UpdateDefinitionBuilder<BillActionData>().Set(x => x.Executed, true);
                //Db.BillActions.UpdateOne(s, x => x.Id == action.Id, upd0);
                t.Wait();
                return;
            }

            var user = Db.UsersCollection.First(x => x.Id == action.UserId);
            
            wallet = new UserWallet()
            {
                Created = DateTime.UtcNow,
                Credits = 0,
                Currency = action.Currency,
                Hidden = true,
                UserId = user.Id,
                BillId = bill.Id,
                JobId = job.Result.Id,
                Purpose = WalletPurpose.Job,
                // put funds
                Funds = bill.TotalAmountAllocated,
            };

            Db.UserWalletsCollection.InsertOne(s, wallet);

            var payments = new List<JobPayment>();
            var dt = DateTime.UtcNow;
            foreach (var p in action.Payments) payments.Add(new JobPayment()
            {
                Currency = action.Currency,
                DateCreated = dt,
                EmployerId = job.Result.EmployerId,
                EmployeeId = job.Result.EmployeeId,
                JobId = job.Result.Id,
                Paid = true,
                StripeIntentId = bill.StripeInvoiceId,
                TempWalletId = wallet.Id,
                Released = false,
                PaymentTitle = p.Key,
                ReceiverFee = 0,
                TransferAmount = p.Value,
            });

            Db.JobPaymentsCollection.InsertMany(s, payments);

            //var upd2 = new UpdateDefinitionBuilder<BillActionData>().Set(x => x.Executed, true);
            //Db.BillActions.UpdateOne(s, x => x.Id == action.Id, upd2);
        }

        public static void Action(this WalletRechargeActionData action, IClientSessionHandle s)
        {
            var user = Db.UsersCollection.First(x => x.Id == action.UserId);
            var transaction = WalletCtl.CreateTransferFromPlatform(user, action.Currency, action.Amount, s: s).Result;

            //var transaction = new WalletTransaction()
            //{
            //    Amount = action.Amount,
            //    DateInitiated = DateTime.UtcNow,
            //    Currency = action.Currency,
            //    ReceiverId = action.UserId,
            //    ReceiverUsername = user.Username,
            //    Status = TransactionStatus.Waiting,
            //    TransactionFeeP = 0,
            //    SenderWalletId = "PLATFORM",
            //    Completed = false,
            //    SenderUsername = "PLATFORM",
            //    UserId = "PLATFORM",
            //    ReceiverWalletId = action.WalletId,
            //};
            //Db.WalletTransactionsCollection.InsertOne(s, transaction);
            //var upd2 = new UpdateDefinitionBuilder<BillActionData>().Set(x => x.Executed, true);
            //Db.BillActions.UpdateOne(s, x => x.Id == action.Id, upd2);
        }

        private static void Action(this SubscriptionActionData action, IClientSessionHandle s)
        {
            var sub = Db.SubscriptionsCollection.Find(s, x => x.Id == action.SubId).FirstOrDefault();

            TimeSpan remainingTime;
            if (sub.ValidUntil <= DateTime.UtcNow) remainingTime = TimeSpan.Zero;
            else remainingTime = sub.ValidUntil - DateTime.UtcNow;

            if (sub.Type != action.Type)
                remainingTime = SubConvert.ConvertSub(remainingTime, sub.Type, action.Type);

            var upd = new UpdateDefinitionBuilder<UserSubscription>()
                .Set(x => x.ValidUntil, DateTime.UtcNow + action.Months.GetMonthTimeSpan() + remainingTime)
                .Set(x => x.Type, action.Type)
                .Set(x => x.NextType, action.Type);

            Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id, upd);

            //var upd2 = new UpdateDefinitionBuilder<BillActionData>().Set(x => x.Executed, true);
            //Db.BillActions.UpdateOne(s, x => x.Id == action.Id, upd2);
        }

        public static void Action(this JobPromoteActionData action, IClientSessionHandle s)
        {
            if (action.IsSearch)
                Db.JobSearchPostsCollection.UpdateOne(s, x => x.Id == action.PostId, new UpdateDefinitionBuilder<JobSearchPost>()
                    .Set(x=>x.Promoted, true));
            else Db.JobPostsCollection.UpdateOne(s, x => x.Id == action.PostId, new UpdateDefinitionBuilder<JobPost>()
                    .Set(x => x.Promoted, true));
        }
    }
}
