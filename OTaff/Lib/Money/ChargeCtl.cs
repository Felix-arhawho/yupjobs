using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class ChargeCtl
    {
        public static Dictionary<string, dynamic> ChargeMethod(
            User user,
            Currency currency,
            UserPaymentMethod method,
            decimal[] amount,
            string desc,
            BillActionData action = null,
            UserBill bill = null,
            bool failKeep = false,
            bool onSession = true)
        {
            if (method is null) return new Dictionary<string, dynamic>() {
                {"error", "You need to provide a payment method" },
                {"paid", false },
                {"redirect", false },
                {"delayed", false }
            };

            switch (method.Type)
            {
                case MethodType.CreditCard:
                    if (onSession)
                        return StripeController.ChargeCustomerOnSession(
                                user: user,
                                amount: amount[1].GetCents(currency),
                                fees: amount,
                                currency: currency,
                                description: desc,
                                method: method,
                                action: action,
                                bill: bill,
                                keepFailed: failKeep).Result;
                    else return StripeController.ChargeCustomerCardOffSession(
                                user: user,
                                amount: amount[1].GetCents(currency),
                                fees: amount,
                                currency: currency,
                                description: desc,
                                method: method,
                                action: action,
                                bill: bill,
                                failKeep: failKeep).Result;
                    break;
                case MethodType.Sofort:
                    return StripeController.ChargeSofort(
                            user,
                            amount[1].GetCents(currency),
                            amount,
                            currency,
                            desc,
                            method,
                            action: action,
                            bill: bill,
                            payIfFailed: false).Result;
                    break;
                case MethodType.StripeInvoice:
                    return StripeController.CreateLinkInvoice(
                        user,
                        DateTime.UtcNow.AddDays(2),
                        currency,
                        amount[1],
                        amount,
                        desc,
                        action: action,
                        bill: bill).Result;
                    break;
                case MethodType.SepaDirect:
                    if (currency != Currency.EUR) return new Dictionary<string, dynamic>()
                    {
                        {"paid", false },
                        {"error", "SEPA Direct can only be used with EUR" }
                    };
                    return StripeController.ChargeSepa(
                        user,
                        amount[1].GetCents(currency),
                        amount,
                        currency,
                        desc,
                        method,
                        action: action,
                        bill: bill,
                        payIfFailed: false).Result;
                    break;
                case MethodType.YupWallet:
                    return ChargeFromWallet(currency, user.Id, amount[1], desc, action); 
                    break;
                default:
                    return new Dictionary<string, dynamic>()
                    {
                        {"paid", false },
                        {"error", "Payment method is not supported" }
                    };
                    break;
            }
        }

        public static Dictionary<string, dynamic> ChargeFromWallet(
            Currency currency, 
            string user, 
            decimal amount, 
            string desc, 
            BillActionData action = null)
        {
            using var s = Db.Client.StartSession();

            s.StartTransaction();
            var wallet = WalletCtl.GetUserWallet(s, user, currency).Result;
            var bill = new UserBill()
            {
                DateIssued = DateTime.UtcNow,
                //Description = desc,
                AutoCharged = true,
                Priority = 0,
                ToPayIfFailed = false,
                UserId = user,
                NextVerif = DateTime.UtcNow+TimeSpan.FromMinutes(2),
                VerifInterval = TimeSpan.FromMinutes(1),
                Currency = currency,
                TotalAmountRequested = amount,
                Framework = PaymentFramework.YupWallet,
                Action = action.ActionType,
                Status = SPaymentStatus.Pending,
                ShouldAct = false,                
            };
            

            var tr = WalletCtl.ChargeWallet(wallet.Id, amount, s, 0.02f, true);
            bill.TransactionId = tr.Result;
            Db.UserBillsCollection.InsertOne(s, bill);
            if (action != null)
            {
                action.BillId = bill.Id;
                Db.BillActions.InsertOne(s, action);
            }

            s.CommitTransaction();

            return new Dictionary<string, dynamic>
            {
                {"bill", bill},
                {"paid", true },
                {"transaction", Db.WalletTransactionsCollection.First(x=>x.Id==tr.Result)}
            };
        }
    }
}
