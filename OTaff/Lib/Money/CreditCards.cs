using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using OTaff.Lib.Extensions;
using ServerLib;

namespace OTaff.Lib.Money
{
    public static partial class StripeController
    {
        public static Task<Dictionary<string, dynamic>> ChargeCustomerOnSession(
            User user,
            Currency currency,
            string description,
            UserPaymentMethod method,
            long amount,
            decimal[] fees,
            BillActionData action = null,
            UserBill bill = null,
            bool keepFailed = false)
        {
            using var s = Db.Client.StartSession();

            s.StartTransaction();
            var newBill = bill is null;
            bill = bill ?? new UserBill()
            {
                AutoCharged = true,
                Paid = false,
                ShouldAct = false,
                ToPayIfFailed = keepFailed,

                TotalAmountRequested = amount.GetMoney(currency),

                Currency = currency,
                MethodType = MethodType.CreditCard,
                Status = SPaymentStatus.Pending,
                Action = action is null ? BillAction.None : action.ActionType,
                //StripeIntentId = intent.Id,
                NextVerif = DateTime.UtcNow + TimeSpan.FromMinutes(30),
                DateIssued = DateTime.UtcNow,
                VerifInterval = TimeSpan.Zero,
                Description = description,
                //StripeIntentId = intent.Id,
                UserId = user.Id,
                TotalFees = fees,
            };

            if (!newBill)
            {
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, new UpdateDefinitionBuilder<UserBill>()
                    //.Set(x => x.StripeIntentId, intent.Id)
                    .Set(x => x.Status, SPaymentStatus.Pending)
                    .Set(x => x.MethodType, MethodType.CreditCard)
                    //.Set(x => x.StripeIntentId, intent.Id)
                    .Set(x => x.Paid, false)
                    .Set(x => x.ShouldAct, false)
                    .Set(x => x.NextVerif, DateTime.MaxValue)
                    .Set(x => x.VerifInterval, TimeSpan.Zero));
            }
            else
            {
                Db.UserBillsCollection.InsertOne(s, bill);
                if (action != null)
                {
                    action.BillId = bill.Id;
                    Db.BillActions.InsertOne(s, action);
                }
            }

            s.CommitTransaction();
            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
            {
                OffSession = false,
                //must capture in confirmation
                CaptureMethod = "manual",
                Amount = amount,
                Confirm = true,
                SetupFutureUsage = "off_session",
                PaymentMethod = method.MethodId,
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                ReturnUrl = $"{SharedLib.Lib.Ez.ClientUrl}/success/pay/{bill.Id}",
                Customer = user.StripeCustomerId,
                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
                ErrorOnRequiresAction = false,
                Description = description,
                ReceiptEmail = user.Email
            };
            var intent = CreatePaymentIntent(options, currency);

            s.StartTransaction();
            var t0 = Db.UserBillsCollection.UpdateOneAsync(
                s, x => x.Id == bill.Id,
                new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.StripeIntentId, intent.Id)
                    .Set(x => x.MethodType, MethodType.CreditCard)
                    .Set(x => x.Captured, false));

            Task.Delay(1000).Wait();
            t0.Wait();

            var ret = new Dictionary<string, dynamic>();
            var upd = new UpdateDefinitionBuilder<UserBill>();

            if (intent.Status is "requires_payment_method")
            {
                ret["redirect"] = false;
                ret["paid"] = false;             

                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd
                    .Set(x => x.Paid, false)
                    .Set(x => x.Status, SPaymentStatus.Fail)
                    .Set(x => x.AutoCharged, false));
            }
            else if (intent.Status is "requires_action")
            {
                ret["redirect"] = true;
                ret["url"] = intent.NextAction.RedirectToUrl.Url;
            }
            else if (intent.Status is "requires_capture")
            {
                try
                {
                    QueueCtl.WaitForTurn(); intent = IntentServiceEU.Capture(intent.Id);
                } catch { }

                ret["redirect"] = false;
                ret["paid"] = intent.Status is "succeeded";
                
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd
                    .Set(x => x.Paid, (bool)ret["paid"])
                    .Set(x => x.ShouldAct, (bool)ret["paid"])
                    .Set(x => x.PaidOn, DateTime.UtcNow)
                    .Set(x => x.Status, SPaymentStatus.Success)
                    .Set(x => x.Captured, true)
                    .Set(x => x.TotalAmountAllocated, intent.AmountReceived.GetMoney(currency)));
            }

            s.CommitTransaction();

            bill = Db.UserBillsCollection.First(x => x.Id == bill.Id);

            ret["bill"] = bill;
            ret["status"] = intent.Status;
            ret["intent"] = intent;

            return Task.FromResult(ret);
        }

        public static async Task<Dictionary<string, dynamic>> ChargeCustomerCardOffSession(
            User user,
            long amount,
            decimal[] fees,
            Currency currency,
            string description,
            UserPaymentMethod method,
            bool failKeep = true,
            BillActionData action = null,
            UserBill bill = null)
        {
            using var s = await Db.Client.StartSessionAsync();

            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
            {
                OffSession = true,
                CaptureMethod = "manual",
                Confirm = true,
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                ReturnUrl = $"https://yupjobs.net/success/pay/",
                Customer = user.StripeCustomerId,
                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
                ErrorOnRequiresAction = true,
                Description = description,
                ReceiptEmail = user.Email,
                Amount = amount,
                PaymentMethod = method.MethodId,
            };

            var intent = CreatePaymentIntent(options, currency);

            // ONCE INTENT CREATED
            Task.Delay(1000).Wait();
            // REGISTER THE BILL

            //DateTime nextVerif = method.Type is MethodType.CreditCard ? DateTime.UtcNow : DateTime.UtcNow+TimeSpan.FromDays(2);
            //TimeSpan verifInterval = method.Type is MethodType.CreditCard ? TimeSpan.Zero : TimeSpan.FromHours(12);

            var newBill = bill is null;
            bill = bill ?? new UserBill()
            {
                AutoCharged = true,
                ToPayIfFailed = failKeep,
                ShouldAct = false,
                Paid = false,

                DateIssued = DateTime.UtcNow,
                NextVerif = DateTime.UtcNow,
                VerifInterval = TimeSpan.Zero,

                UserId = user.Id,
                Description = description,
                StripeIntentId = intent.Id,

                TotalFees = fees,
                TotalAmountRequested = amount.GetMoney(currency),
                MethodType = MethodType.CreditCard,
                Currency = currency,
                Status = SPaymentStatus.Pending,
                Action = action is null ? BillAction.None : action.ActionType,
            };

            s.StartTransaction();
            if (newBill)
            {
                Db.UserBillsCollection.InsertOne(s, bill);

                //action.BillId = bill.Id;
                //Db.BillActions.InsertOne(s, action);
            }
            else
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.StripeIntentId, intent.Id)
                    //.Set(x => x.StripeInvoiceId, intent.Invoice.Id)
                    .Set(x => x.Status, SPaymentStatus.Pending)
                    .Set(x => x.MethodType, MethodType.CreditCard)
                    .Set(x => x.Paid, false)
                    .Set(x => x.ShouldAct, !(action is null))
                    .Set(x => x.NextVerif, DateTime.UtcNow)
                    .Set(x => x.VerifInterval, TimeSpan.Zero);

                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }

            if (action != null)
            {
                action.BillId = bill.Id;
                action.Executed = false;
                action.Issued = DateTime.UtcNow;

                Db.BillActions.InsertOne(s, action);
            }

            s.CommitTransaction();


            QueueCtl.WaitForTurn(); 
            intent = IntentServiceEU.Capture(intent.Id);

            bill = Db.UserBillsCollection.First(x => x.Id == bill.Id);

            var status =
                intent.Status is "succeeded" ?
                SPaymentStatus.Success
                : SPaymentStatus.Fail;


            //bill.MethodType = MethodType.CreditCard;
            bill.Paid = status is SPaymentStatus.Success;
            bill.Status = status;
            bill.PaidOn = status is SPaymentStatus.Success ? new DateTime?(DateTime.UtcNow) : null;
            bill.TotalAmountAllocated = status is SPaymentStatus.Fail ? 0 : intent.AmountReceived.GetMoney(currency);
            bill.ToPayIfFailed = failKeep;
            bill.ShouldAct = status is SPaymentStatus.Success;

            if (status != SPaymentStatus.Success && bill.ToPayIfFailed)
            {
                bill.FailCount++;
                if (bill.FailCount > 2)
                {
                    bill.FailFees.Add(0.5m);
                }
            }
            Db.UserBillsCollection.ReplaceOne(x => x.Id == bill.Id, bill);

            return new Dictionary<string, dynamic>()
            {
                {"redirect", false },
                {"paid", status == SPaymentStatus.Success},
                {"intent", intent },
                {"bill", bill }
            };
        }
    }
}
