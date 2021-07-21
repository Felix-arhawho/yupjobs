using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using OTaff.Lib.Money;
using OTaff.Lib.Extensions;
using SharedLib.Lib;
using SharedLib.Models;

namespace WorkerApp.Workers
{
    public static class IntervalPaymentsWorker
    {
        public static async Task CreatePayments()
        {
            while (true)
            {
                Console.WriteLine($"[WORKER][PAYMENT INTERVALS] New round started");
                try
                {
                    using var cursor = Db.IntervalPaymentOrders.FindSync(x => x.Active && !x.PaymentFailed && x.NextPayment < DateTime.UtcNow, new FindOptions<IntervalPaymentOrder, IntervalPaymentOrder>() 
                    { 
                        Sort = new SortDefinitionBuilder<IntervalPaymentOrder>().Ascending(x=>x.NextPayment),
                        BatchSize = 100
                    });
                    
                    while (cursor.MoveNext())
                    {
                        if (cursor.Current.ToList().Count is 0)
                        {
                            await Task.Delay(1000 * 30);
                            continue;
                        }
                        foreach (var i in cursor.Current)
                        {
                            var user = Db.UsersCollection.First(x => x.Id == i.EmployeeId);
                            using var s = Db.Client.StartSession();
                            var fees = FeesCtl.WalletRechargeFee(i.Payment, i.Currency);
                            s.StartTransaction();
                            var charge = ChargeCtl.ChargeMethod(
                                Db.UsersCollection.First(x => x.Id == i.EmployerId),
                                i.Currency,
                                Db.PaymentMethodsCollection.First(x => x.Id == i.PaymentMethodId),
                                fees,
                                $"Reccuring payment for {user.Username} for order n.{i.Id}",
                                new JobPaymentActionData
                                {
                                    Description = $"Reccuring payment for {user.Username} for order n.{i.Id}",
                                    ActionType = BillAction.AddJobPayment,
                                    AutoRelease = true,
                                    Currency = i.Currency,
                                    EmployeeId = i.EmployeeId,
                                    EmployerId = i.EmployerId,
                                    Issued = DateTime.UtcNow,
                                    Executed = false,
                                    JobId = i.JobId,
                                    Payments = new Dictionary<string, decimal> { { $"Reccuring payment for {user.Username}", i.Payment } },
                                    UserId = i.EmployerId,
                                },
                                onSession: false);

                            if (charge["paid"])
                            {
                                Db.IntervalPaymentOrders.UpdateOne(s, x => x.Id == i.Id, new UpdateDefinitionBuilder<IntervalPaymentOrder>()
                                    .Set(x => x.LastPayment, DateTime.UtcNow)
                                    .Set(x => x.NextPayment, DateTime.UtcNow + i.PaymentInterval));

                                Db.NotificationsCollections.InsertOne(s, new Notification
                                {
                                    Date = DateTime.UtcNow,
                                    Description = "You have been paid " + i.Payment + " " + i.Currency + " for job " + i.JobId,
                                    Href = "/job/ongoing/" + i.JobId,
                                    Title = "Payment received",
                                    UserId = i.EmployeeId,
                                });

                                s.CommitTransaction();
                            }
                            else
                            {
                                Db.IntervalPaymentOrders.UpdateOne(s, x => x.Id == i.Id, new UpdateDefinitionBuilder<IntervalPaymentOrder>()
                                    .Set(x => x.PaymentFailed, true));
                                s.CommitTransaction();
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(1000 * 60 * 2);
            }






        }
        public static async Task Payout()
        {

        }
    }
}
