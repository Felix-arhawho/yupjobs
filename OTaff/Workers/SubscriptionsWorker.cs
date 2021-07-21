
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
    public class SubLock
    {
        public string Id = string.Empty;
        //public string 
    }

    public static class SubscriptionsWorker
    {
        private static bool _working = false;
        public static bool Start { get => _working; set {
                _working = value;
                if (value) _ = CheckSubs();
            } }

        private static SynchronizedCollection<SubLock> CurrentSubs = new SynchronizedCollection<SubLock>();

        public static async Task CheckSubs(bool work = true)
        {
            Console.WriteLine("[WORKER][SUBSCRIPTION] Subscriptions worker has started");

            _working = work;

            //using 
            while (_working)
            {
                try
                {
                    await Task.Delay(5000);
                    Console.WriteLine($"[WORKER][SUBSCRIPTION] Starting new subscriptions worker round");

                    using var cursor = Db.SubscriptionsCollection.FindSync(
                        x => x.ValidUntil <= DateTime.UtcNow+TimeSpan.FromDays(3) && !x.OngoingRecharge, 
                        new FindOptions<UserSubscription, UserSubscription>() { 
                            Sort = new SortDefinitionBuilder<UserSubscription>().Ascending(x=>x.ValidUntil),
                            BatchSize = 200
                        });

                    var tls = new Task[0];
                    while (cursor.MoveNext() && _working)
                    {
                        foreach (var sub in cursor.Current)
                        {
                            var t = Task.Run(async () =>
                            {
                                if (Db.SubBillActions.CountDocuments(x => x.SubId == sub.Id && !x.Executed && x.Months > 0) > 0)
                                    return;

                                var s = Db.Client.StartSession();
                                var user = Db.UsersCollection.Find(s, x => x.Id == sub.UserId).FirstOrDefault();
                                var method = Db.PaymentMethodsCollection.Find(s, x => sub.PaymentMethodId == x.Id).FirstOrDefault();

                                s.StartTransaction();

                                if (!sub.Renew)
                                {
                                    var upd = new UpdateDefinitionBuilder<UserSubscription>()
                                        .Set(x => x.Type, SubscriptionType.Free)
                                        .Set(x => x.NextType, SubscriptionType.Free)
                                        .Set(x => x.PaymentMethodId, null);
                                    Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id, upd);

                                    s.CommitTransaction();
                                    return;
                                }

                                var price = SubscriptionsMeta.SubscriptionCosts[sub.NextType];
                                var convPrice = CurrencyConversion.GetConvertedRate(price, sub.Currency);

                                var ret = ChargeCtl.ChargeMethod(
                                        user,
                                        sub.Currency,
                                        method,
                                        new[] { 0m, convPrice, 0m},
                                        $"{Enum.GetName(typeof(SubscriptionType), sub.Type)} subscription recharge from YupJobs",
                                        action: new SubscriptionActionData()
                                        {
                                            Description = "Subscription recharge from YupJobs",
                                            ActionType = BillAction.Subscription,
                                            Executed = false,
                                            Issued = DateTime.UtcNow,
                                            UserId = user.Id,
                                            Months = 1,
                                            SubId = sub.Id,
                                            Type = sub.NextType,
                                        }, onSession: false);

                                if (!((bool)ret["paid"] || (bool)ret["processing"]))
                                {
                                    var upd = new UpdateDefinitionBuilder<UserSubscription>()
                                        .Set(x => x.Type, SubscriptionType.Free)
                                        .Set(x => x.NextType, SubscriptionType.Free)
                                        .Set(x => x.PaymentMethodId, null);
                                    Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id, upd);

                                    s.CommitTransaction();
                                    return;
                                }
                                else
                                {
                                    //vqr upd
                                }

                                s.CommitTransaction();
                            });
                            tls.Append(t);
                        }
                        Task.WaitAll(tls);
                        tls = new Task[0];
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

}
