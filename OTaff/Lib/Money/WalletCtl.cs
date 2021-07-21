using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class WalletCtl
    {
        public static WalletTransaction WaitForTransactionCompletion(string id)
        {
            var transaction = Db.WalletTransactionsCollection.First(x => x.Id == id);
            while (!transaction.Completed) 
            {
                Task.Delay(5000).Wait();
                transaction = Db.WalletTransactionsCollection.First(x => x.Id == id);
            }

            return transaction;
        }

        public static async Task<UserWallet> GetUserWallet(string userId, Currency currency)
        {
            try
            {
                var user = await Db.UsersCollection.FirstAsync(x => x.Id == userId);
                var wallet = Db.UserWalletsCollection.First(x => x.UserId == userId && x.Currency == currency && !x.Hidden);

                if (user is null) return null;
                if (wallet is null)
                {
                    wallet = new UserWallet()
                    {
                        Created = DateTime.UtcNow,
                        Funds = 0,
                        Currency = currency,
                        UserId = userId,
                        Purpose = WalletPurpose.General
                    };
                    Db.UserWalletsCollection.InsertOne(wallet);
                }
                return wallet;
            }
            catch
            {
                return null;
            }
        }
        public static Task<UserWallet> GetUserWallet(IClientSessionHandle s, string userId, Currency currency)
        {
            try
            {
                var user = Db.UsersCollection.First(x => x.Id == userId);
                if (user is null) return null;
                var wallet = Db.UserWalletsCollection.First(x => x.UserId == userId && x.Currency == currency && !x.Hidden);
                if (wallet is null)
                {
                    wallet = new UserWallet()
                    {
                        Created = DateTime.UtcNow,
                        Funds = 0,
                        Currency = currency,
                        UserId = userId,
                        Purpose = WalletPurpose.General
                    };
                    Db.UserWalletsCollection.InsertOne(s, wallet);
                }
                return Task.FromResult(wallet);
            }
            catch
            {
                return Task.FromResult((UserWallet)null);
            }
        }

        public static async Task<string> ChargeWallet(string walletId, decimal amount, IClientSessionHandle s = null, float fee = 0.02f, bool normal = false)
        {
            var newS = s is null;
            if (newS)
            {
                s = Db.Client.StartSession();
                s.StartTransaction();
            }

            try
            {
                var wallet = Db.UserWalletsCollection.First(x => x.Id == walletId);
                var user = Db.UsersCollection.First(x => x.Id == wallet.UserId);

                var transaction = new WalletTransaction
                {
                    Currency = wallet.Currency,
                    DateInitiated = DateTime.UtcNow,
                    Type = normal ? TransactionType.Transfer : TransactionType.Payout,
                    ReceiverUsername = "PLATFORM",
                    ReceiverId = "PLATFORM",
                    ReceiverWalletId = "PLATFORM",
                    Status = TransactionStatus.Waiting,
                    SenderUsername = user.Username,
                    SenderWalletId = wallet.Id,
                    Amount = amount,
                    Priority = 0,
                };
                Db.WalletTransactionsCollection.InsertOne(s, transaction);

                if (newS)
                    s.CommitTransaction();
                
                return transaction.Id;
            }
            catch (Exception e)
            {
                s.AbortTransaction();
                Console.WriteLine(e);
                return null;
            }
            finally
            {
                if (newS)
                    s.Dispose();
            }
        }

        public static async Task<bool> CreateTransfer(User target, string originWalletId, decimal amount, float fee = 0.02f, bool hide = false, IClientSessionHandle s = null)
        {
            bool localSession = s is null;
            try
            {
                var originWallet = Db.UserWalletsCollection.First(x => x.Id == originWalletId);

                //if (amount > originWallet.Funds + originWallet.Credits)
                //    return false;

                s = s ?? await Db.Client.StartSessionAsync();

                var originUser = await Db.UsersCollection.Find(x => x.Id == originWallet.UserId).FirstOrDefaultAsync();
                //var wallet = Db.UserWalletsCollection.Find(x => x.UserId == target.Id && originWallet.Currency == x.Currency && !x.Hidden).FirstOrDefault();
                
                if (localSession)
                    s.StartTransaction();


                var wallet = await GetUserWallet(s, target.Id, originWallet.Currency);

                var transaction = new WalletTransaction()
                {
                    DateInitiated = DateTime.UtcNow,
                    Status = TransactionStatus.Waiting,
                    Completed = false,
                    Amount = amount,
                    Currency = originWallet.Currency,
                    ReceiverId = target.Id,
                    SenderUsername = originUser.Username,
                    ReceiverUsername = target.Username,
                    ReceiverWalletId = wallet.Id,
                    TransactionFeeP = fee,
                    SenderWalletId = originWallet.Id,
                    UserId = originUser.Id,
                    Priority = 2,
                    Type = TransactionType.Transfer,
                };

                Db.WalletTransactionsCollection.InsertOne(s, transaction);


                Console.WriteLine($"Created transfer of {amount} {originWallet.Currency} to {target.Username}");

                if (localSession)
                {
                    s.CommitTransaction();
                    s.Dispose();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create transaction towards " + target.Username);
                if (localSession)
                {
                    s.AbortTransaction();
                    s.Dispose();
                }

                Console.WriteLine(e);
                return false;
            }
        }

        public static async Task<bool> CreateTransferFromPlatform(User target, Currency currency, decimal amount, float fee = 0, IClientSessionHandle s = null)
        {
            bool isLocal = s is null;
            s = s ?? await Db.Client.StartSessionAsync();
            try
            {
                //var wallet = Db.UserWalletsCollection.Find(x => x.UserId == target.Id && currency == x.Currency && !x.Hidden).First();
                if (isLocal)
                    s.StartTransaction();
                var wallet = await GetUserWallet(s, target.Id, currency);
                if (wallet is null) throw new Exception($"Could not find or create a wallet for recharge to user: {target.Username}");
                //if (wallet is null)
                //{
                //    wallet = new UserWallet()
                //    {
                //        Created = DateTime.UtcNow,
                //        Currency = currency,
                //        Funds = 0,
                //        Hidden = false,
                //        UserId = target.Id,
                //        Purpose = WalletPurpose.General,
                //    };
                //    Db.UserWalletsCollection.InsertOne(s, wallet);
                //}
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
                var transaction = new WalletTransaction()
                {
                    DateInitiated = DateTime.UtcNow,
                    Priority = 2,
                    Type = TransactionType.Recharge,
                    Status = TransactionStatus.Waiting,

                    UserId = "PLATFORM",
                    SenderWalletId = "PLATFORM",
                    ReceiverWalletId = wallet.Id,
                    SenderUsername = "PLATFORM",
                    ReceiverId = target.Id,
                    ReceiverUsername = target.Username,

                    Currency = currency,
                    Amount = amount,
                    TransactionFeeP = fee,
                };
                Db.WalletTransactionsCollection.InsertOne(s, transaction);

                if (isLocal)
                    s.CommitTransaction();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (isLocal)
                {
                    s.AbortTransaction();
                    s.Dispose();
                }
                return false;
            }
        }

        //public static async Task<Dictionary<string, dynamic>> RechargeWallet(string walletId, string userId, decimal amount)
        //{
        //    var user = await Db.UsersCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        //    var wallet = await Db.UserWalletsCollection.Find(x => x.Id == walletId).FirstOrDefaultAsync();

        //    var fee = amount * (5m / 100m);
        //    var toReceiver = amount - fee;

        //    //TODO CHARGE CUSTOMER an invoice
        //    var transaction = new WalletTransaction()
        //    {
        //        Completed = false,
        //        ReceiverWalletId = wallet.Id,
        //        TransactionFeeP = 0.02f,
        //        DateInitiated = DateTime.UtcNow,
        //        Status = TransactionStatus.Waiting,
        //        ReceiverUsername = user.Username,
        //        SenderUsername = "PLATFORM",
        //        SenderWallet = "PLATFORM",
        //        ReceiverId = user.Id,
        //        UserId = "PLATFORM"
        //    };

        //    Db.WalletTransactionsCollection.InsertOne(transaction);
        //    //

        //    return new Dictionary<string, dynamic>() 
        //    {
        //        //{  }
        //    };
        //}


    }


}
