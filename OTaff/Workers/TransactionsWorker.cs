using OTaff.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using SharedLib.Models;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OTaff.Lib.Extensions;
using Stripe;
using OTaff.Lib.Money;
using ServerLib;
using ServerLib.Models;
using SharedLib.Lib;

namespace OTaff.Workers
{
    public static class TransactionsWorker
    {
        private static bool _working = false;
        public static bool Start
        {
            get => _working;
            set
            {
                _working = value;
                if (value)
                {
                    _ = DoWork();
                    _ = Cleanup();
                }
            }
        }

        private static object transactionLock = new object();

        static TransferService TransferServiceEU = new TransferService();
        static TransferService TransferServiceIN = new TransferService();

        public static async Task DoWork()
        {
            Console.WriteLine("[WORKER] Started transactions worker");
            var stw = new Stopwatch();

            short trCount = 0;

            while (true)
            {
                IAsyncCursor<WalletTransaction> cursor = null;
                try
                {
                    Console.WriteLine("Restarting transaction round");
                    stw.Restart();

                    cursor = Db.WalletTransactionsCollection.FindSync(x => !x.Completed && x.Status == TransactionStatus.Waiting, new FindOptions<WalletTransaction, WalletTransaction>()
                    {
                        Sort = new SortDefinitionBuilder<WalletTransaction>()
                            .Ascending(x => x.Priority)
                            .Ascending(x => x.DateInitiated),
                        BatchSize = 100
                    });

                    List<string> ChangedWalletIds = new List<string>();
                    bool taskFinished = true;

                    //var st = new Stopwatch();
                    while (cursor.MoveNext())
                    {
                        if (cursor.Current.Count() is 0) await Task.Delay(1000 * 5);
                        foreach (var transaction in cursor.Current)
                        {
                            var s = await Db.Client.StartSessionAsync();
                            try
                            {
                                Console.WriteLine($"Starting transaction {transaction.Id}");
                                var user = transaction.UserId is "PLATFORM" ? null : await Db.UsersCollection.Find(x => x.Id == transaction.UserId).FirstOrDefaultAsync();
                                var senderWallet = transaction.SenderWalletId is "PLATFORM" ? null : await Db.UserWalletsCollection.Find(x => x.Id == transaction.SenderWalletId).FirstOrDefaultAsync();
                                var receiverWallet = transaction.ReceiverWalletId is "PLATFORM" ? null : await Db.UserWalletsCollection.Find(x => x.Id == transaction.ReceiverWalletId).FirstOrDefaultAsync();

                                s.StartTransaction();
                                //if (receiverWallet) 

                                lock (transactionLock)
                                {
                                    //senderWallet = ChangedWalletIds.Contains(senderWallet.Id) ? Db.UserWalletsCollection.Find(s, x => x.UserId == transaction.UserId).FirstOrDefault();
                                    //receiverWallet = ChangedWalletIds.Contains(senderWallet.Id) ? Db.UserWalletsCollection.Find(s, x => x.UserId == transaction.UserId).FirstOrDefault();


                                    if (transaction.SenderWalletId is "PLATFORM")
                                    {
                                        var preceiveAmount = transaction.Amount;

                                        var pupd = new UpdateDefinitionBuilder<UserWallet>()
                                            .Set(x => x.Funds, receiverWallet.Funds + preceiveAmount);

                                        Db.WalletTransactionsCollection.UpdateOne(s, x => x.Id == transaction.Id, new UpdateDefinitionBuilder<WalletTransaction>()
                                            .Set(x => x.Status, TransactionStatus.Processed)
                                            .Set(x => x.Completed, true)
                                            .Set(x => x.DateCompleted, DateTime.UtcNow));

                                        Db.UserWalletsCollection.UpdateOne(s, x => x.Id == receiverWallet.Id, pupd);

                                        s.CommitTransaction();
                                        //Console.WriteLine($"Completed recharge {transaction.Id} - {transaction.Amount} {Enum.GetName(typeof(Currency), transaction.Currency)} to {transaction.ReceiverUsername}");
                                        continue;
                                    }
                                    if (transaction.ReceiverId is "PLATFORM")
                                    {
                                        //var upd = new UpdateDefinitionBuilder<WalletTransaction>()
                                        //    .Set(x => x.Status, TransactionStatus.Processed)
                                        //    .Set(x => x.Completed, true)
                                        //    .Set(x => x.DateCompleted, DateTime.UtcNow);

                                        if (senderWallet.Funds < transaction.Amount)
                                        {
                                            Db.WalletTransactionsCollection.UpdateOne(s, x => x.Id == transaction.Id, new UpdateDefinitionBuilder<WalletTransaction>()
                                            .Set(x => x.Status, TransactionStatus.Failed)
                                            .Set(x => x.FailReason, "Not enough funds in the wallet")
                                            .Set(x => x.Completed, true)
                                            .Set(x => x.DateCompleted, DateTime.UtcNow));

                                            s.CommitTransaction();
                                            continue;
                                        }

                                        Db.WalletTransactionsCollection.UpdateOne(s, x => x.Id == transaction.Id, new UpdateDefinitionBuilder<WalletTransaction>()
                                            .Set(x => x.Status, TransactionStatus.Processed)
                                            .Set(x => x.Completed, true)
                                            .Set(x => x.DateCompleted, DateTime.UtcNow));

                                        var pupd = new UpdateDefinitionBuilder<UserWallet>()
                                            .Set(x => x.Funds, senderWallet.Funds - transaction.Amount);

                                        Db.UserWalletsCollection.UpdateOne(s, x => x.Id == senderWallet.Id, pupd);

                                        if (transaction.Type is TransactionType.Payout)
                                        {
                                            s.CommitTransaction();
                                            s.Dispose();

                                            Task.Run(delegate
                                            {
                                                Console.WriteLine($"Doing payout for {transaction.Id}");

                                                var action = Db.BankPayoutActions.First(x => x.TransactionId == transaction.Id && !x.Executed);

                                                try
                                                {
                                                    var eurAmt = FeesCtl.ConvertToEur(transaction.Currency, transaction.Amount);

                                                    Transfer transfer;
                                                    QueueCtl.WaitForTurn();
                                                    //if (transaction.Currency is Currency.INR)
                                                    //    transfer = TransferServiceIN.Create(new TransferCreateOptions
                                                    //    {
                                                    //        //Currency = Enum.GetName(transaction.Currency).ToLowerInvariant(),
                                                    //        Currency = Enum.GetName(Currency.INR).ToLower(),
                                                    //        Amount = transaction.Amount.GetCents(Currency.INR),
                                                    //        //Amount = transaction.Amount.GetCents(transaction.Currency),
                                                    //        Destination = action.ConnectAccountId,
                                                    //    });
                                                    //else 
                                                        transfer = TransferServiceEU.Create(new TransferCreateOptions
                                                    {
                                                        //Currency = Enum.GetName(transaction.Currency).ToLowerInvariant(),
                                                        Currency = Enum.GetName(Currency.EUR).ToLower(),
                                                        Amount = eurAmt.GetCents(Currency.EUR),
                                                        //Amount = transaction.Amount.GetCents(transaction.Currency),
                                                        Destination = action.ConnectAccountId,
                                                    });

                                                    var updB = new UpdateDefinitionBuilder<PayoutToBank>()
                                                        .Set(x => x.Executed, true)
                                                        .Set(x => x.DateCompleted, DateTime.UtcNow);

                                                    Db.BankPayoutActions.UpdateOne(x => x.Id == action.Id, updB);

                                                    Db.NotificationsCollections.InsertOneAsync(new Notification 
                                                    {
                                                        UserId = transaction.UserId,
                                                        Date = DateTime.UtcNow,
                                                        Description = "Payout of "+ action.Amount/100 + " " + action.Currency.GetName() +" has been completed",
                                                        Title = "Payout initiated",
                                                        Href = "https://dashboard.stripe.com/",
                                                    });
                                                }
                                                catch (StripeException e)
                                                {
                                                    Console.WriteLine(e);
                                                    var trn = WalletCtl.CreateTransferFromPlatform(
                                                        Db.UsersCollection.First(x => x.Id == transaction.UserId),
                                                        transaction.Currency,
                                                        transaction.Amount);
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine(e);
                                                }

                                            });
                                        }
                                        else
                                        {
                                            s.CommitTransaction();
                                            s.Dispose();
                                        }

                                        continue;
                                    }

                                    //CHECK FOR ERRORS
                                    if (senderWallet.Currency != receiverWallet.Currency)
                                    {
                                        var upd = new UpdateDefinitionBuilder<WalletTransaction>()
                                            .Set(x => x.Completed, true)
                                            .Set(x => x.FailReason, "Target wallet is not of the same currency")
                                            .Set(x => x.Status, TransactionStatus.Failed)
                                            .Set(x => x.DateCompleted, DateTime.UtcNow);
                                        Db.WalletTransactionsCollection.UpdateOne(s, x => x.Id == transaction.Id, upd);
                                        s.CommitTransaction();

                                        continue;
                                    }

                                    if (senderWallet.Funds < transaction.Amount)
                                    {
                                        var upd = new UpdateDefinitionBuilder<WalletTransaction>()
                                            .Set(x => x.Completed, true)
                                            .Set(x => x.FailReason, "Not enough funds in the wallet")
                                            .Set(x => x.Status, TransactionStatus.Failed)
                                            .Set(x => x.DateCompleted, DateTime.UtcNow);
                                        Db.WalletTransactionsCollection.UpdateOne(s, x => x.Id == transaction.Id, upd);
                                        s.CommitTransaction();

                                        continue;
                                    }

                                    //start transaction
                                    var supd = new UpdateDefinitionBuilder<UserWallet>()
                                        .Set(x => x.Funds, senderWallet.Funds - transaction.Amount);

                                    var fees = transaction.Amount * (decimal)transaction.TransactionFeeP;
                                    var receiveAmount = transaction.Amount - fees;

                                    var rupd = new UpdateDefinitionBuilder<UserWallet>()
                                        .Set(x => x.Funds, receiverWallet.Funds + receiveAmount);

                                    var t1 = Db.UserWalletsCollection.UpdateOneAsync(s, x => x.Id == senderWallet.Id, supd);
                                    var t2 = Db.UserWalletsCollection.UpdateOneAsync(s, x => x.Id == receiverWallet.Id, rupd);
                                    var t3 = Db.WalletTransactionsCollection.UpdateOneAsync(s, x => x.Id == transaction.Id, new UpdateDefinitionBuilder<WalletTransaction>()
                                        .Set(x => x.Status, TransactionStatus.Processed)
                                        .Set(x => x.Completed, true)
                                        .Set(x => x.DateCompleted, DateTime.UtcNow));

                                    var t4 = Db.FeesCollection.InsertOneAsync(new FeeData
                                    {
                                        Currency = transaction.Currency,
                                        Units = fees,
                                        Type = FeeType.Transaction,
                                        MetaId = transaction.Id,
                                        Date = DateTime.UtcNow,
                                    });

                                    t1.Wait();
                                    t2.Wait();
                                    t3.Wait();
                                    t4.Wait();

                                    s.CommitTransaction();

                                    //st.Start();
                                    Console.WriteLine($"Completed transaction {transaction.Id} - {transaction.Amount} {Enum.GetName(typeof(Currency), transaction.Currency)} to {transaction.ReceiverUsername}");
                                    //s.Dispose();
                                }

                                trCount++;
                            }
                            catch (Exception e)
                            {
                                s.AbortTransaction();
                                //s.Dispose();
                                Console.WriteLine("Transaction error is " + e);
                            }
                            finally
                            {
                                s.Dispose();
                            }
                        }
                    }

                    stw.Stop();

                    Console.WriteLine($"Proccessed {trCount} transactions in {stw.Elapsed.TotalSeconds} seconds");
                    trCount = 0;
                }
                catch (Exception e)
                {
                    cursor.Dispose();
                    Console.WriteLine(e);
                }

                //await Task.Delay(10000);
            }
        }
        public static async Task Cleanup()
        {
            using var s = Db.Client.StartSession();
            while (_working)
            {
                s.StartTransaction();
                var cursor = Db.WalletTransactionsCollection.FindSync(s, x => x.DateCompleted < DateTime.UtcNow - TimeSpan.FromDays(60), new FindOptions<WalletTransaction, WalletTransaction>
                {
                    BatchSize = 200,
                });

                var ls = new List<string>();
                while (cursor.MoveNext())
                {
                    if (cursor.Current.Count() is 0) break;

                    Db.WalletTransactionsArchives.InsertMany(s, cursor.Current);
                    ls.AddRange(cursor.Current.Select(x => x.Id));
                }

                Db.WalletTransactionsCollection.DeleteMany(s, x => ls.Contains(x.Id));
                s.CommitTransaction();

                await Task.Delay(1000 * 60 * 30);
            }
        }
    } 
}
