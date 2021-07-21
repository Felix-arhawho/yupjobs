
using OTaff.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using OTaff.Lib.Money;
using Stripe;
using SharedLib.Models;
using OTaff.Lib.Extensions;
using ServerLib;

namespace WorkerApp.Workers
{
    public static class PayoutsWorker
    {
        public static bool Work = true;
        public static Task DoWork()
        {
            while (Work)
            {
                try
                {
                    using var cursor = Db.BankPayoutActions.FindSync(x => !x.Executed, new FindOptions<PayoutToBank, PayoutToBank>
                    {
                        Sort = new SortDefinitionBuilder<PayoutToBank>().Ascending(x => x.DateCreated)
                    });
                    while (cursor.MoveNext())
                    {
                        var tls = new List<Task>();
                        foreach (var payout in cursor.Current) tls.Add(Task.Run(async () =>
                        {
                            using var s = await Db.Client.StartSessionAsync();
                            s.StartTransaction();

                            var options = new PayoutCreateOptions()
                            {
                                Currency = Enum.GetName(typeof(Currency), payout.Currency),
                                Method = "standard",
                                Description = "Payout from YupJobs",
                                StatementDescriptor = "Payout from YupJobs",
                                Amount = payout.Amount.GetCents(payout.Currency),
                                //Destination = 
                            };

                            Payout spayout;
                            lock (StripeAccounts.ConnectLock) spayout = StripeAccounts.PayoutService.Create(options);


                            s.CommitTransaction();
                        }));
                    }
                }
                catch
                {

                }

            }

            return Task.CompletedTask;
        }
        public static async Task PayoutAction(this PayoutToBank action, IClientSessionHandle s)
        {
            //var wallet = Db.UserWalletsCollection.First(x => action.WalletId == x.Id);

            var transactionId = await WalletCtl.ChargeWallet(action.WalletId, action.Amount);
            if (transactionId is null) return;
            var transaction = WalletCtl.WaitForTransactionCompletion(transactionId);

            var options = new TransferCreateOptions
            {
                Amount = action.Amount.GetCents(action.Currency),
                Currency = Enum.GetName(typeof(Currency), action.Currency),
                Destination = action.ConnectAccountId,
            };

            Transfer transfer;
            Payout payout;

            lock (StripeAccounts.ConnectLock) transfer = StripeAccounts.TransferService.Create(options);
            lock (StripeAccounts.ConnectLock) payout = StripeAccounts.PayoutService.Create(new PayoutCreateOptions
            {
                StatementDescriptor = "Payout from YupJobs",
                Currency = Enum.GetName(typeof(Currency), action.Currency).ToLower(),
                Method = "standard",
                Amount = action.Amount.GetCents(action.Currency)
            });
            //lock (StripeAccounts.ConnectLock) payout = StripeAccounts.PayoutService.Create(options2); 
        }
    }
}
