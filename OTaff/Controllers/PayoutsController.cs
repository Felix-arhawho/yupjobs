using Microsoft.AspNetCore.Mvc;
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
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/payouts")]
    [ApiController]
    public class PayoutsController : ControllerBase
    {
        [HttpPost("newprofile")]
        public ActionResult<string> RegisterConnectAccount(
            [FromForm] string token)
        {
            try
            {
                var jwt = DeserializeObject<Jwt>(token);
                if (!jwt.Verify()) return BadRequest("NOAUTH");

                var user = jwt.GetUser();
                //var method = Db.PaymentMethodsCollection.Find(x => x.Id == methodId).FirstOrDefault();
                //if (method is null || method.UserId != user.Result.Id) return BadRequest("Payment method not found");

                var cacc = StripeAccounts.CreateConnectAccount(user);
                
                return cacc.ToJson();
            }
            catch (Exception e)
            {
                return UnprocessableEntity("An unknown error has occured");
            }
        }



        //Tuple<string, int, decimal, User, Jwt> tuple = new Tuple<string, int, decimal, User, Jwt>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost("accountstatus")]
        public async Task<ActionResult<string>> AccountStatus(
            [FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var ret = new Dictionary<string, dynamic>();

            var payoutAccount = Db.ConnectAccounts.First(x => x.UserId == jwt.UserId);

            if (payoutAccount is null)
            {
                ret["exists"] = false;
                return ret.ToJson();
            }

            QueueCtl.WaitForTurn();
            Account stripeAccount = StripeAccounts.AccountServiceEU.Get(payoutAccount.ConnectId);

            ret["exists"] = true;
            ret["account"] = payoutAccount;

            if (stripeAccount.DetailsSubmitted)
                ret["valid"] = true;
            else ret["valid"] = false;

            return ret.ToJson();
        }

        /// <summary>
        /// Returns a link to stripe connect onboard form, if no account exists it returns link to register the connect account 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="edit"></param>
        /// <returns></returns>
        [HttpPost("getlink")]
        public ActionResult<string> GetLink([FromForm] string token, [FromForm] bool edit = false)
        {
            try
            {
                var jwt = DeserializeObject<Jwt>(token);
                if (!jwt.Verify()) return Unauthorized();

                var user = Db.ConnectAccounts.Find(x => x.UserId == jwt.UserId).FirstOrDefault();

                var options = new AccountLinkCreateOptions
                {
                    Account = user.ConnectId,
                    RefreshUrl = "https://yupjobs.net/success",
                    ReturnUrl = "https://yupjobs.net/success",
                    Type = "account_onboarding",
                };
                var service = new AccountLinkService();
                var link = service.Create(options);



                //if (user is null) return "/payouts/register";

                //var link = edit ? StripeAccounts.GetAccountEditLink(user.ConnectId) : StripeAccounts.GetAccountOnboardLink(user.ConnectId);

                return link.Url;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                return UnprocessableEntity("An unknown error has occured");
            }
        }

        LoginLinkService ServiceEU = new LoginLinkService();
        LoginLinkService ServiceIN = new LoginLinkService();


        [HttpPost("myloginlink")]
        public ActionResult<string> LoginLink([FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var usr = jwt.GetUserAsync();
            var user = Db.ConnectAccounts.Find(x => x.UserId == jwt.UserId).FirstOrDefaultAsync();
            
            QueueCtl.WaitForTurn();
            if (user.Result is null) return BadRequest();

            if (usr.Result.Country is CountryCode.IN)
                return ServiceIN.Create(user.Result.ConnectId, new LoginLinkCreateOptions { RedirectUrl = "https://yupjobs.net/" }).Url;
            else return ServiceEU.Create(user.Result.ConnectId, new LoginLinkCreateOptions { RedirectUrl = "https://yupjobs.net/" }).Url;
        }

        [HttpPost("transfer")]
        public async Task<ActionResult<string>> Transfer([FromForm] string token, [FromForm] string walletId, [FromForm] decimal amount = 0)
        {
            try
            {
                var jwt = token.ToObject<Jwt>();
                if (!jwt.Verify()) return Unauthorized();

                using var s = await Db.Client.StartSessionAsync();
                var wallet = await Db.UserWalletsCollection.FirstAsync(x => x.Id == walletId);
                var payoutAccount = await Db.ConnectAccounts.FirstAsync(x => x.UserId == jwt.UserId);
                s.StartTransaction();
                var transaction = new WalletTransaction()
                {
                    Amount = amount,
                    Currency = wallet.Currency,
                    Status = TransactionStatus.Waiting,
                    DateInitiated = DateTime.UtcNow,
                    ReceiverUsername = "PLATFORM",
                    ReceiverWalletId = "PLATFORM",
                    SenderUsername = jwt.Username,
                    Completed = false,
                    ReceiverId = "PLATFORM",
                    SenderWalletId = wallet.Id,
                    TransactionFeeP = 0.05f,
                    Type = TransactionType.Payout,
                    UserId = jwt.UserId,
                };
                Db.WalletTransactionsCollection.InsertOne(s, transaction);

                var action = new PayoutToBank()
                {
                    Currency = wallet.Currency,
                    Amount = amount.GetCents(wallet.Currency),
                    DateCreated = DateTime.UtcNow,
                    ConnectAccountId = payoutAccount.ConnectId,
                    Executed = false,
                    UserId = jwt.UserId,
                    WalletId = wallet.Id,
                    TransactionId = transaction.Id,
                };
                Db.TransactionActions.InsertOne(s, action);

                s.CommitTransaction();

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest();
            }
        }
    }
}
