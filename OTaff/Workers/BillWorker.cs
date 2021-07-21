using MongoDB.Driver;
using OTaff.Lib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using System.Threading;
using System.Diagnostics;
using ServerLib;

namespace OTaff.Workers
{
    public static partial class BillWorker
    {
        public static bool Start { get => _doWork; set {
                _doWork = value;
            } }
        static bool _doWork { get; set; } = true;

        public static decimal SofortFailFee = 1.5m;
        public static decimal SepaFailFee = 1.5m;
        public static decimal CardFailFee = 2m;

        public static async Task ActionWork()
        {
            while (_doWork)
            {
                try
                {
                    Console.WriteLine("[WORKER][BILLING] Starting new Bill Actions worker round");

                    using var cursor = Db.UserBillsCollection.FindSync(
                    x => x.ShouldAct,
                    new FindOptions<UserBill, UserBill>()
                    {
                        Sort = new SortDefinitionBuilder<UserBill>().Ascending(x => x.DateIssued),
                        BatchSize = 100,
                    });

                    var tls = new Task[0];
                    while (cursor.MoveNext() && _doWork)
                    {
                        foreach (var bill in cursor.Current)
                        {
                            var t = Task.Run(delegate
                            {
                                using var s = Db.Client.StartSession();
                                try
                                {
                                    s.StartTransaction();
                                    var action = Db.BillActions.Find(x => x.BillId == bill.Id).FirstOrDefault();
                                    if (action is null)
                                    {
                                        Console.WriteLine($"Action for bill {bill.Id} was null");
                                    }
                                    else
                                    {
                                        switch (action.ActionType)
                                        {
                                            case BillAction.Subscription:
                                                (action as SubscriptionActionData).Action(s);
                                                break;
                                            case BillAction.RechargeWallet:
                                                (action as WalletRechargeActionData).Action(s);
                                                break;
                                            case BillAction.AddJobPayment:
                                                (action as JobPaymentActionData).Action(bill, s);
                                                break;
                                            case BillAction.Promote:
                                                (action as JobPromoteActionData).Action(s);
                                                break;
                                        }
                                    }
                                    Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, new UpdateDefinitionBuilder<UserBill>()
                                        .Set(x => x.ShouldAct, false));
                                    Db.BillActions.UpdateOne(s, x => x.Id == action.Id, new UpdateDefinitionBuilder<BillActionData>()
                                        .Set(x => x.Executed, true)
                                        .Set(x => x.ExecutedOn, DateTime.UtcNow));

                                    s.CommitTransaction();
                                }
                                catch (Exception e)
                                {
                                    s.AbortTransaction();
                                    Console.WriteLine(e);
                                }
                            });
                            tls.Append(t);
                        }

                        Task.WaitAll(tls);
                        tls = new Task[0];

                        await Task.Delay(5000);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static async Task PayCheck()
        {
            var tls = new List<Task>();
            long cnt = 0;
            long invoices = 0;
            long intents = 0;

            var s = new Stopwatch();

            while (_doWork)
            {
                try
                {
                    Console.WriteLine("[WORKER][BILLING] Starting new Bill checking worker round");

                    var cursor = Db.UserBillsCollection.FindSync(
                        x => !x.Paid && x.Status == SPaymentStatus.Pending,
                        new FindOptions<UserBill, UserBill>
                        {
                            BatchSize = 100,
                            Sort = new SortDefinitionBuilder<UserBill>()
                                .Ascending(x => x.LastVerif)
                                .Ascending(x => x.DateIssued)
                        });
                    s.Start();
                    while (cursor.MoveNext())
                    {
                        foreach (var bill in cursor.Current)
                        {
                            var t = Task.Run(delegate
                            {
                                Interlocked.Increment(ref cnt);
                                //using var s = Db.Client.StartSession();
                                //s.StartTransaction();

                                if (bill.Framework is PaymentFramework.Invoice)
                                {
                                    HandleInvoice(bill);
                                    Interlocked.Increment(ref invoices);
                                }
                                else if (bill.Framework is PaymentFramework.Intents)
                                {
                                    HandleIntent(bill);
                                    Interlocked.Increment(ref intents);
                                }else if (bill.Framework is PaymentFramework.YupWallet)
                                {
                                    HandleWallet(bill);
                                }
                                //s.CommitTransaction();
                            });

                            tls.Add(t);
                        }
                        await Task.WhenAll(tls);
                        tls.Clear();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    s.Stop();
                    Console.WriteLine($"[WORKER] Bill payments checker has processed {cnt} bills, {invoices} invoices, {intents} intents in {s.Elapsed.Seconds} seconds");
                    cnt = 0;
                    invoices = 0;
                    intents = 0;
                    s.Reset();
                }
                
                await Task.Delay(5000);
            }
        }

        private static void HandleIntent(UserBill bill, IClientSessionHandle s = null)
        {
            s = Db.Client.StartSession();
            s.StartTransaction();

            if (string.IsNullOrWhiteSpace(bill.StripeIntentId))
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.FailCount, bill.FailCount + 1)
                    .Set(x => x.Paid, false)
                    .Set(x => x.Captured, false)
                    .Set(x => x.Status, SPaymentStatus.MethodRequired)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.NextVerif, DateTime.MaxValue)
                    .Set(x => x.ShouldAct, false);
                Db.UserBillsCollection.UpdateOne(x => x.Id == bill.Id, upd);
                return;
            }

            PaymentIntent intent;
            QueueCtl.WaitForTurn();
            //if (bill.Currency is Currency.INR)
            //    intent = StripeController.IntentServiceIN.Get(bill.StripeIntentId);
            intent = StripeController.IntentServiceEU.Get(bill.StripeIntentId);

            if (intent.Status is "succeeded")
                bill.SetPaid(ref intent, s);
            else if (intent.Status is "processing")
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.Paid, false)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.NextVerif, DateTime.UtcNow + bill.VerifInterval)
                    .Set(x => x.ShouldAct, false);
                    //.Set(x => x.InvoiceUrl, );

                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }
            else if (intent.Status is "requires_payment_method")
            {
                var failFees = 0;
                for (int i = 1; i < bill.FailCount; i++)
                {
                    bill.FailFees.Add(CurrencyConversion.GetConvertedRate(0.5m, bill.Currency, 0));
                }
                
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.FailCount, bill.FailCount + 1)
                    .Set(x => x.FailFees, bill.FailFees)
                    .Set(x => x.Paid, false)
                    .Set(x => x.Status, SPaymentStatus.MethodRequired)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.ShouldAct, false)
                    .Set(x => x.NextVerif, DateTime.UtcNow + bill.VerifInterval); ;
                
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }

            s.CommitTransaction();
            s.Dispose();
        }
        
        private static void HandleInvoice(UserBill bill, IClientSessionHandle s = null)
        {
            s = Db.Client.StartSession();
            s.StartTransaction();

            if (string.IsNullOrWhiteSpace(bill.StripeInvoiceId))
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.FailCount, bill.FailCount + 1)
                    .Set(x => x.Paid, false)
                    .Set(x => x.Captured, false)
                    .Set(x => x.Status, SPaymentStatus.MethodRequired)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.NextVerif, DateTime.MaxValue);
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
                return;
            }

            //Invoice invoice;
            //lock (StripeController.InvoiceLock) 
            QueueCtl.WaitForTurn();
            Invoice invoice;
            //if (bill.Currency is Currency.INR)
                //invoice = StripeController.InvoiceServiceIN.Get(bill.StripeInvoiceId);
            invoice = StripeController.InvoiceServiceEU.Get(bill.StripeInvoiceId);

            if (invoice.Paid)
                bill.SetPaid(ref invoice, s);
            else if (invoice.Status is "uncollectible" || invoice.Status is "void")
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.FailCount, bill.FailCount + 1)
                    .Set(x => x.Paid, false)
                    .Set(x => x.Captured, false)
                    .Set(x => x.ShouldAct, false)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.Status, SPaymentStatus.Fail)
                    .Set(x => x.NextVerif, DateTime.UtcNow + bill.VerifInterval);
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }
            else
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.VerifsCount, bill.VerifsCount+1)
                    .Set(x => x.VerifInterval, bill.VerifsCount > 10 ? TimeSpan.FromHours(12) : bill.VerifInterval)
                    .Set(x => x.LastVerif, DateTime.UtcNow)
                    .Set(x => x.Status, SPaymentStatus.Pending)
                    .Set(x => x.NextVerif, DateTime.UtcNow + bill.VerifInterval);
                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd);
            }

            s.CommitTransaction();
            s.Dispose();
        }

        private static void HandleWallet(UserBill bill)
        {
            var tr = WalletCtl.WaitForTransactionCompletion(bill.TransactionId);
            if (tr.Status == TransactionStatus.Processed)
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x=>x.Paid, true)
                    .Set(x=>x.Status, SPaymentStatus.Success)
                    .Set(x=>x.PaidOn, DateTime.UtcNow)
                    .Set(x=>x.TotalAmountAllocated, tr.Amount)
                    .Set(x=>x.ShouldAct, true);
                Db.UserBillsCollection.UpdateOne(x => x.Id == bill.Id, upd);
            }
            else
            {
                var upd = new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.Paid, false)
                    .Set(x => x.Status, SPaymentStatus.Fail)
                    .Set(x => x.ShouldAct, true);
                Db.UserBillsCollection.UpdateOne(x => x.Id == bill.Id, upd);
            }
        }

        //public static class SubConvertRatios
        //{
        //    public static float PersoToPro = 0.4f;
        //    public static float PersoToBiz = 0.25f;
        //    public static float ProToPerso = 1.4f;
        //    public static float ProToBiz = 0.4f;
        //    public static float BizToPerso = 1.8f;
        //    public static float BizToPro = 1.4f;
        //}

        //public static TimeSpan ConvertSub(TimeSpan time, SubscriptionType baseType, SubscriptionType targetType)
        //{
        //    var newTime = TimeSpan.Zero;

        //    if (baseType is SubscriptionType.Personal && targetType is SubscriptionType.Pro)
        //    {
        //        newTime = time * SubConvertRatios.PersoToPro;
        //    }
        //    else if (baseType is SubscriptionType.Personal && targetType is SubscriptionType.Business)
        //    {
        //        newTime = time * SubConvertRatios.PersoToBiz;
        //    }
        //    else if (baseType is SubscriptionType.Pro && targetType is SubscriptionType.Personal)
        //    {
        //        newTime = time * SubConvertRatios.ProToPerso;
        //    }
        //    else if (baseType is SubscriptionType.Pro && targetType is SubscriptionType.Business)
        //    {
        //        newTime = time * SubConvertRatios.ProToBiz;
        //    }
        //    else if (baseType is SubscriptionType.Business && targetType is SubscriptionType.Personal)
        //    {
        //        newTime = time * SubConvertRatios.BizToPerso;
        //    }
        //    else if (baseType is SubscriptionType.Business && targetType is SubscriptionType.Pro)
        //    {
        //        newTime = time * SubConvertRatios.BizToPro;
        //    }

        //    return newTime;
        //}


    }
}
