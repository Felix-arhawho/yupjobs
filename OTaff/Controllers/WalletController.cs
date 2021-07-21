using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Lib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/wallet")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        [HttpPost("get")]
        public ActionResult<string> GetAllWallets(
            [FromForm] string token,
            [FromForm] string id = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            if (id is null)
            {
                var wallets = Db.UserWalletsCollection.Find(x => x.UserId == jwt.UserId && !x.Hidden).ToList();
                var tls = new List<Task>();
                foreach (var wallet in wallets) tls.Add(Task.Run(delegate
                {
                    var transactions = Db.WalletTransactionsCollection.Find(x => x.SenderWalletId == wallet.Id || x.ReceiverWalletId == wallet.Id).ToList();

                    wallet.OutgoingFunds = transactions.FindAll(x => x.SenderWalletId == wallet.Id && !x.Completed).Select(x => x.Amount).Sum();
                    wallet.IncomingFunds = transactions.FindAll(x => x.ReceiverWalletId == wallet.Id && !x.Completed).Select(x => x.Amount).Sum();
                    wallet.AvailableFunds = wallet.Funds - wallet.OutgoingFunds;
                }));
                Task.WaitAll(tls.ToArray());
                return wallets.ToJson();
            }
            else
            {
                var wallet = Db.UserWalletsCollection.First(x => x.Id == id);
                var transactions = Db.WalletTransactionsCollection.Find(x => x.SenderWalletId == wallet.Id || x.ReceiverWalletId == wallet.Id).ToList();

                wallet.OutgoingFunds = transactions.FindAll(x => x.SenderWalletId == wallet.Id && !x.Completed).Select(x => x.Amount).Sum();
                wallet.IncomingFunds = transactions.FindAll(x => x.ReceiverWalletId == wallet.Id && !x.Completed).Select(x => x.Amount).Sum();
                wallet.AvailableFunds = wallet.Funds - wallet.OutgoingFunds;

                return wallet.ToJson();
            }
        }

        /// <summary>
        /// different client handles needed based on type
        /// </summary>
        /// <param name="token"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="methodId"></param>
        /// <returns></returns>
        [HttpPost("recharge")]
        public async Task<ActionResult<string>> Recharge(
            [FromForm] string token,
            [FromForm] decimal amount,
            [FromForm] Currency currency,
            [FromForm] string methodId)
        {
            try
            {
                var jwt = token.ToObject<Jwt>();
                if (!jwt.Verify()) return Unauthorized();

                using var s = await Db.Client.StartSessionAsync();
                var method = await Db.PaymentMethodsCollection.Find(x => x.Id == methodId && x.UserId == jwt.UserId).FirstOrDefaultAsync();
                var user = await jwt.GetUserAsync();
                if (method is null) return BadRequest("Invalid payment method");

                Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();

                s.StartTransaction();

                var wallet = Db.UserWalletsCollection.First(x => x.UserId == jwt.UserId && x.Currency == currency && !x.Hidden);
                if (wallet is null)
                {
                    wallet = new UserWallet()
                    {
                        Created = DateTime.UtcNow,
                        Funds = 0,
                        UserId = jwt.UserId,
                        Hidden = false,
                        Currency = currency,
                        Purpose = WalletPurpose.General,
                    };
                    Db.UserWalletsCollection.InsertOne(s, wallet);
                }

                var fees = FeesCtl.WalletRechargeFee(amount, currency);

                var action = new WalletRechargeActionData
                {
                    ActionType = BillAction.RechargeWallet,
                    Currency = currency,
                    Amount = amount,
                    Executed = false,
                    Issued = DateTime.UtcNow,
                    UserId = jwt.UserId,
                    WalletId = wallet.Id
                };

                s.CommitTransaction();

                ret = ChargeCtl.ChargeMethod(
                    user,
                    currency,
                    method,
                    fees,
                    $"Recharge of {Math.Round(amount, 2)} {currency.GetName()} for {user.Username}",
                    action: action);

                return ret.ToJson();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest("Payment method is not supported");
            }
        }

        [HttpPost("transactions")]
        public ActionResult<string> Transactions([FromForm] string token, [FromForm] string walletId = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var transactions = Db.WalletTransactionsCollection.All(x => x.UserId == jwt.UserId || x.ReceiverId == jwt.UserId);
            return transactions.ToJson();
        }

        //[HttpPost("confirm3ds")]
        //public void Confirm3DSecure([FromForm] string token, [FromForm] string billId)
        //{
        //    var jwt = DeserializeObject<Jwt>(token);

        //    var user = jwt.GetUserAsync();
        //    var bill = Db.UserBillsCollection.Find(x => x.Id==billId).FirstOrDefault();
            
        //    if (bill is null || bill.UserId != user.Result.Id) return;
        
        //    PaymentIntent intent;
        //    lock (StripeController.CustomerLock) intent = StripeController.IntentService.Get(bill.StripeIntentId);
        //    if (!intent.Invoice.Paid) return;
        //    Db.UserBillsCollection.UpdateOne(x => x.Id == billId, new UpdateDefinitionBuilder<UserBill>().Set(x => x.Paid, true).Set(x => x.PaidOn, DateTime.UtcNow));
        //}

        

    }

    //public class 

    public class WalletRechargeTask
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string WalletId { get; set; }
        public string UserId { get; set; }
        public string PaymentId { get; set; }
        public string BillId { get; set; }
        public MethodType MethodType { get; set; }
        public decimal RawAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime DateInitiated { get; set; }
        public TimeSpan TimeToVerif { get; set; }
        public DateTime LastVerif { get; set; }
        public bool Paid { get; set; }
        public DateTime PaidOn { get; set; }
    }
}
