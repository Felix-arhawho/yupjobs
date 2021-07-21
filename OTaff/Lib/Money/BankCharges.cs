using MongoDB.Driver;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static partial class StripeController
    {
        public static async Task<Dictionary<string, dynamic>> ChargeSofort(
            User user, 
            long amount, 
            decimal[] fees,
            Currency currency, 
            string description,
            UserPaymentMethod method,
            bool payIfFailed = false,
            BillActionData action = null, 
            UserBill bill = null)
        {
            using var s = await Db.Client.StartSessionAsync();

            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
            {
                //OffSession = false,
                //CaptureMethod = "automatic",
                Amount = amount,
                Confirm = false,
                PaymentMethod = method.MethodId,
                PaymentMethodTypes = new List<string>
                {
                    "sofort",
                },
                //ReturnUrl = $"https://yupjobs.net/success/pay",
                Customer = user.StripeCustomerId,
                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
                //ErrorOnRequiresAction = false,
                Description = description,
                ReceiptEmail = user.Email,
                CaptureMethod = "automatic",
                
            };

            var intent = CreatePaymentIntent(options, currency);

            var newBill = bill is null;
            bill = bill ?? new UserBill()
            {
                DateIssued = DateTime.UtcNow,
                Framework = PaymentFramework.Intents,
                Description = description,
                UserId = user.Id,
                Paid = false,
                Status = SPaymentStatus.Pending,
                AutoCharged = false,
                NextVerif = DateTime.UtcNow.AddDays(1),
                VerifInterval = TimeSpan.FromHours(12),
                Action = action is null ? BillAction.None : action.ActionType,
                ShouldAct = false,
                Currency = currency,
                MethodType = MethodType.Sofort,
                ToPayIfFailed = payIfFailed,
                StripeIntentId = intent.Id,
                TotalFees = fees,
            };

            if (newBill)
            {
                Db.UserBillsCollection.InsertOne(s, bill);

                action.BillId = bill.Id;
                Db.BillActions.InsertOne(s, action);
            }
            else
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.StripeIntentId, intent.Id)
                    .Set(x => x.NextVerif, DateTime.UtcNow.AddDays(1))
                    .Set(x => x.VerifInterval, TimeSpan.FromHours(12))
                    .Set(x => x.MethodType, MethodType.Sofort);
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }

            s.CommitTransaction();

            QueueCtl.WaitForTurn();
            intent = IntentServiceEU.Confirm(intent.Id, new PaymentIntentConfirmOptions
            {
                ReturnUrl = $"{SharedLib.Lib.Ez.ClientUrl}success/pay/{bill.Id}",
                ReceiptEmail = user.Email,
                OffSession = false,
            });

            //Db.UserBillsCollection.UpdateOne(x => x.Id == bill.Id, upd);

            return new Dictionary<string, dynamic>() 
            {
                {"bill", bill },
                {"status", intent.Status },
                {"redirect", true },
                {"url", intent.NextAction.RedirectToUrl.Url },
                {"intent", intent },
            };
        }

        public static async Task<Dictionary<string, dynamic>> ChargeSepa(
            User user,
            long amount,
            decimal[] fees,
            Currency currency,
            string description,
            UserPaymentMethod method,
            bool payIfFailed = false,
            BillActionData action = null,
            UserBill bill = null)
        {
            using var s = await Db.Client.StartSessionAsync();

            PaymentIntentCreateOptions options = new PaymentIntentCreateOptions()
            {
                //OffSession = false,
                //CaptureMethod = "automatic",
                Amount = amount,
                Confirm = false,
                PaymentMethod = method.MethodId,
                PaymentMethodTypes = new List<string>
                {
                    "sepa_debit",
                },
                //ReturnUrl = $"https://yupjobs.net/success/pay",
                Customer = user.StripeCustomerId,
                Currency = Enum.GetName(typeof(Currency), currency).ToUpper(),
                //ErrorOnRequiresAction = true,
                Description = description,
                //ReceiptEmail = user.Email,
                //OffSession = true,

                CaptureMethod = "automatic"
            };

            var intent = CreatePaymentIntent(options, currency);

            s.StartTransaction();
            var newBill = bill is null;
            bill = bill ?? new UserBill()
            {
                DateIssued = DateTime.UtcNow,
                Framework = PaymentFramework.Intents,
                Description = description,
                UserId = user.Id,
                Paid = false,
                Status = SPaymentStatus.Pending,
                AutoCharged = false,
                NextVerif = DateTime.UtcNow.AddDays(1),
                VerifInterval = TimeSpan.FromHours(12),
                Action = action is null ? BillAction.None : action.ActionType,
                ShouldAct = false,
                Currency = currency,
                MethodType = MethodType.SepaDirect,
                ToPayIfFailed = payIfFailed,
                StripeIntentId = intent.Id,
                TotalFees = fees
            };

            if (newBill) {
                Db.UserBillsCollection.InsertOne(s, bill);

                if (action != null)
                {
                    action.BillId = bill.Id;
                    Db.BillActions.InsertOne(s, action);
                }
            }
            else 
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.StripeIntentId, intent.Id)
                    .Set(x => x.NextVerif, DateTime.UtcNow.AddDays(1))
                    .Set(x => x.VerifInterval, TimeSpan.FromHours(12))
                    .Set(x => x.MethodType, MethodType.SepaDirect);
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }

            s.CommitTransaction();


            QueueCtl.WaitForTurn();
            intent = IntentServiceEU.Confirm(intent.Id, new PaymentIntentConfirmOptions { 
                ReturnUrl = $"https://yupjobs.net/success/pay/{bill.Id}",
                ReceiptEmail = user.Email,
                OffSession = true,
                MandateData = new PaymentIntentMandateDataOptions()
                {
                    CustomerAcceptance = new PaymentIntentMandateDataCustomerAcceptanceOptions()
                    {
                        AcceptedAt = DateTime.UtcNow,
                        Type = "offline",
                    }
                },
            });

            return new Dictionary<string, dynamic>
            {
                {"status", intent.Status },
                {"redirect", false },
                {"paid", false },
                {"processing", intent.Status is "processing" },
                {"bill", bill },
                {"intent", intent },
            };
        }
    }
}
