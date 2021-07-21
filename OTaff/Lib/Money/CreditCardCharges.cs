//using SharedLib.Models;
//using Stripe;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using MongoDB.Driver;
//using OTaff.Lib.Extensions;

//namespace OTaff.Lib.Money
//{
//    public static partial class StripeController
//    {
//        public static async Task<Dictionary<string, dynamic>> ChargeCustomerOnSession(
//            User user,
//            long amount,
//            Currency currency,
//            string description,
//            UserPaymentMethod method,
//            BillActionData action = null,
//            UserBill bill = null,
//            bool keepFailed = false)
//        {
//            using var s = await Db.Client.StartSessionAsync();

//            s.StartTransaction();

//            var newBill = bill is null;
//            bill = bill ?? new UserBill()
//            {
//                AutoCharged = true,
//                Paid = false,
//                ShouldAct = false,
//                ToPayIfFailed = keepFailed,

//                TotalAmountRequested = amount,

//                Currency = currency,
//                MethodType = MethodType.CreditCard,
//                Status = SPaymentStatus.Pending,
//                Action = action is null ? BillAction.None : action.ActionType,

//                NextVerif = DateTime.UtcNow + TimeSpan.FromMinutes(30),
//                DateIssued = DateTime.UtcNow,
//                VerifInterval = TimeSpan.Zero,

//                Description = description,
//                //StripeIntentId = intent.Id,
//                UserId = user.Id,
//            };

//            if (!newBill)
//            {
//                var upd = new UpdateDefinitionBuilder<UserBill>()
//                    //.Set(x => x.StripeIntentId, intent.Id)
//                    .Set(x => x.Status, SPaymentStatus.Pending)
//                    .Set(x => x.MethodType, MethodType.CreditCard)
//                    .Set(x => x.Paid, false)
//                    .Set(x => x.ShouldAct, false)
//                    .Set(x => x.NextVerif, DateTime.UtcNow)
//                    .Set(x => x.VerifInterval, TimeSpan.Zero);

//                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
//            }
//            else Db.UserBillsCollection.InsertOne(s, bill);

//            s.CommitTransaction();

//            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
//            {
//                OffSession = false,

//                //must capture in confirmation
//                CaptureMethod = "manual",
//                Amount = amount,
//                Confirm = true,
//                SetupFutureUsage = "off_session",
//                PaymentMethod = method.MethodId,
//                PaymentMethodTypes = new List<string>
//                {
//                    "card",
//                },
//                ReturnUrl = $"https://yupjobs.net/success/pay/{bill.Id}",
//                Customer = user.StripeCustomerId,
//                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
//                ErrorOnRequiresAction = false,
//                Description = description,
//                ReceiptEmail = user.Email,
//                //SetupFutureUsage = "off_session"
//            };


//            var intent = CreatePaymentIntent(options);
//            var t0 = Db.UserBillsCollection.UpdateOneAsync(
//                s, x => x.Id == bill.Id,
//                new UpdateDefinitionBuilder<UserBill>()
//                    .Set(x => x.StripeIntentId, intent.Id)
//                    .Set(x => x.MethodType, MethodType.CreditCard));

//            Task.Delay(1000).Wait();
//            await t0;

//            var ret = new Dictionary<string, dynamic>();
//            if (intent.Status is "requires_payment_method")
//            {
//                ret["redirect"] = false;
//                ret["paid"] = false;
//                var upd = new UpdateDefinitionBuilder<UserBill>()
//                    .Set(x => x.Paid, false)
//                    .Set(x => x.Status, SPaymentStatus.Fail)
//                    .Set(x => x.AutoCharged, false);
//                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
//            }
//            else if (intent.Status is "requires_action")
//            {
//                ret["redirect"] = true;
//                ret["url"] = intent.NextAction.RedirectToUrl.Url;
//            }
//            //else if (intent.Status is "succeeded")
//            //{
//            //    ret["redirect"] = false;
//            //    ret["paid"] = true;    
//            //}
//            else if (intent.Status is "requires_capture")
//            {
//                lock (InvoiceLock) intent = IntentService.Capture(intent.Id);
//                ret["redirect"] = false;
//                ret["paid"] = intent.Invoice?.Paid;
//                //ret["capture"] 

//                var upd = new UpdateDefinitionBuilder<UserBill>()
//                    .Set(x => x.Paid, true)
//                    .Set(x => x.PaidOn, DateTime.UtcNow)
//                    .Set(x => x.Status, SPaymentStatus.Success);
//                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
//            }


//            bill.Refresh();

//            //= intent.Status is "requires_capture";
//            ret["status"] = intent.Status;
//            ret["intent"] = intent;
//            ret["bill"] = bill;

//            return ret;
//        }

//        /// <summary>
//        /// /// Returns status and bill Id
//        /// </summary>
//        /// <param name="user"></param>
//        /// <param name="options"></param>
//        /// <param name="description"></param>
//        /// <returns></returns>
//        public static async Task<Dictionary<string, dynamic>> ChargeCustomerCardOffSession(
//            User user,
//            long amount,
//            Currency currency,
//            string description,
//            UserPaymentMethod method,
//            bool failKeep = true,
//            BillActionData action = null,
//            UserBill bill = null)
//        {
//            using var s = Db.Client.StartSession();

//            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
//            {
//                OffSession = true,
//                CaptureMethod = "manual",
//                Confirm = true,
//                PaymentMethodTypes = new List<string>
//                {
//                    "card",
//                },
//                ReturnUrl = $"https://yupjobs.net/success/pay/card",
//                Customer = user.StripeCustomerId,
//                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
//                ErrorOnRequiresAction = true,
//                Description = description,
//                ReceiptEmail = user.Email,
//                Amount = amount,
//                PaymentMethod = method.MethodId
//            };

//            var intent = CreatePaymentIntent(options);

//            // ONCE INTENT CREATED
//            Task.Delay(1000).Wait();
//            // REGISTER THE BILL

//            //DateTime nextVerif = method.Type is MethodType.CreditCard ? DateTime.UtcNow : DateTime.UtcNow+TimeSpan.FromDays(2);
//            //TimeSpan verifInterval = method.Type is MethodType.CreditCard ? TimeSpan.Zero : TimeSpan.FromHours(12);

//            var newBill = bill is null;
//            bill = bill ?? new UserBill()
//            {
//                AutoCharged = true,
//                ToPayIfFailed = failKeep,
//                ShouldAct = false,
//                Paid = false,

//                DateIssued = DateTime.UtcNow,
//                NextVerif = DateTime.UtcNow,
//                VerifInterval = TimeSpan.Zero,

//                UserId = user.Id,
//                Description = description,
//                StripeIntentId = intent.Id,

//                MethodType = MethodType.CreditCard,
//                Currency = currency,
//                Status = SPaymentStatus.Pending,
//                Action = action is null ? BillAction.None : action.ActionType,
//            };

//            s.StartTransaction();
//            if (newBill) Db.UserBillsCollection.InsertOne(s, bill);
//            else
//            {
//                var upd = new UpdateDefinitionBuilder<UserBill>()
//                    .Set(x => x.StripeIntentId, intent.Id)
//                    .Set(x => x.StripeInvoiceId, intent.Invoice.Id)
//                    .Set(x => x.Status, SPaymentStatus.Pending)
//                    .Set(x => x.MethodType, MethodType.CreditCard)
//                    .Set(x => x.Paid, false)
//                    .Set(x => x.ShouldAct, false)
//                    .Set(x => x.NextVerif, DateTime.UtcNow)
//                    .Set(x => x.VerifInterval, TimeSpan.Zero);

//                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
//            }

//            if (action != null)
//            {
//                action.BillId = bill.Id;
//                action.Executed = false;
//                action.Issued = DateTime.UtcNow;

//                Db.BillActions.InsertOne(s, action);
//            }

//            s.CommitTransaction();

//            lock (CustomerLock) intent = IntentService.Capture(intent.Id);

//            bill.Refresh();

//            var status =
//                intent.Status is "succeeded" ?
//                SPaymentStatus.Success
//                : SPaymentStatus.Fail;


//            //bill.MethodType = MethodType.CreditCard;
//            bill.Paid = status is SPaymentStatus.Success;
//            bill.Status = status;
//            bill.PaidOn = status is SPaymentStatus.Success ? new DateTime?(DateTime.UtcNow) : null;
//            bill.TotalAmountAllocated = status is SPaymentStatus.Fail ? 0 : Math.Round(intent.Invoice.AmountPaid / 100m, 2, MidpointRounding.ToPositiveInfinity);
//            bill.ToPayIfFailed = failKeep;

//            if (status != SPaymentStatus.Success && bill.ToPayIfFailed)
//            {
//                bill.FailCount++;
//                if (bill.FailCount > 2)
//                {
//                    bill.FailFees.Add(0.5m);
//                }
//            }
//            Db.UserBillsCollection.ReplaceOne(x => x.Id == bill.Id, bill);

//            return new Dictionary<string, dynamic>()
//            {
//                {"paid", intent.Invoice.Paid},
//                {"intent", intent },
//                {"bill", bill }
//            };
//        }
//    }
//}
