using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/money")]
    [ApiController]
    public class MoneyController : ControllerBase
    {
        private static CustomerService customerService = new CustomerService();

        /// <summary>
        /// Checks account for a valid payment method
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost("check")]
        public ActionResult<string> Check([FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            if (Db.PaymentMethodsCollection.CountDocuments(x => x.UserId == jwt.UserId) is 0)
                return NotFound("No method found");
            return Ok("Has methods");
        }


        [HttpPost("findmethods")]
        public ActionResult<string> FindMethods([FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            return Db.PaymentMethodsCollection.All(x => x.UserId == jwt.UserId).ToJson();
        }

        [HttpPost("addcard")]
        public ActionResult<string> AddCard(
            [FromForm] string token,
            [FromForm] string number,
            [FromForm] short expMonth,
            [FromForm] short expYear,
            [FromForm] string cvc,
            [FromForm] string name,
            [FromForm] string address = null,
            [FromForm] string city = null,
            [FromForm] string zipcode = null,
            [FromForm] string state = null,
            [FromForm] string country = null,
            //optional
            [FromForm] string cardemail = null,
            [FromForm] string cardphone = null)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var user = Db.UsersCollection.Find(x => x.Id == jwt.UserId).FirstOrDefault();

            var options = new PaymentMethodCreateOptions() {
                Customer = user.StripeCustomerId,
                Type = "card",
                BillingDetails = new PaymentMethodBillingDetailsOptions()
                {
                    //Address = new AddressOptions()
                    //{
                    //    Line1 = address,
                    //    City = city,
                    //    PostalCode = zipcode,
                    //    State = state,
                    //    Country = country
                    //},
                    Email = cardemail ?? user.Email,
                    Name = name,
                    Phone = cardphone
                },
                Card = new PaymentMethodCardOptions()
                {
                    Number = number,
                    ExpMonth = expMonth,
                    ExpYear = expYear,
                    Cvc = cvc,
                },
            };

            var method = StripeController.AddPaymentCard(options);
            var dbmethod = new UserPaymentMethod()
            {
                MethodId = method.Id,
                UserId = user.Id,
                Type = MethodType.CreditCard,
                OwnerName = name,
                MetaName = $"Ending in {new string(number.TakeLast(4).ToArray())}"
            };

            Db.PaymentMethodsCollection.InsertOne(dbmethod);

            return Ok(dbmethod.Id);
        }

        //[HttpPost("addsofort")]
        //public ActionResult<string> AddSofort()
        //{

        //}

        [HttpPost("addsepa")]
        public ActionResult<string> AddSepa(
            [FromForm] string token,
            [FromForm] string iban,
            [FromForm] string name,
            [FromForm] bool signed)
        {
            if (!signed) return BadRequest("You need to accept the debit mandate otherwise we cannot debit funds from your account");

            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            var user = jwt.GetUser();

            var options = new PaymentMethodCreateOptions()
            {
                Customer = user.StripeCustomerId,
                Type = "sepa_debit",
                SepaDebit = new PaymentMethodSepaDebitOptions()
                {
                    Iban = iban,
                },
                BillingDetails = new PaymentMethodBillingDetailsOptions()
                {
                    Name = name,
                    Email = jwt.Email
                }
            };

            PaymentMethod smethod = null;

            try
            {
                smethod = StripeController.CreatePaymentMethod(options);
            } catch (StripeException e)
            {
                //Console.WriteLine($"\n\n\n {e}");
                return BadRequest("Please verify the information");
            } catch (Exception e)
            {
                return StatusCode(500, "Internal error");
            }


            var dbmethod = new UserPaymentMethod()
            {
                Default = false,
                MetaName = $"Bank account ending in {new string(iban.TakeLast(6).ToArray())}",
                MethodId = smethod.Id,
                OwnerName = name,
                Type = MethodType.SepaDirect,
                UserId = jwt.UserId,
            };

            Db.PaymentMethodsCollection.InsertOne(dbmethod);
            return dbmethod.ToJson();
        }

        [HttpPost("remove")]
        public ActionResult<string> RemoveMethod(
            [FromForm] string token,
            [FromForm] string id)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var method = Db.PaymentMethodsCollection.Find(x => x.Id == id && x.UserId == jwt.UserId).FirstOrDefault();
            if (method is null) return NotFound("This payment method does not exist");

            lock (StripeController.PaymentMethodLock) StripeController.PaymentMethodService.Detach(method.MethodId);
            Db.PaymentMethodsCollection.DeleteOne(x => x.Id == id);

            return Ok();
        }

        [HttpGet("subscriptionprices")]
        public ActionResult<string> SubPrices(Currency currency)
        {
            //DDoS

            return new JObject()
            {
                {"notice", "These prices are based on EUR and currency exchange rates are applied, if you are not paying in EUR the price for each subscription might fluctuate by a small amount at each renewal" },
                {"currency", Enum.GetName(typeof(Currency), currency) },
                {"personal", CurrencyConversion.GetConvertedRate(SubscriptionsMeta.SubscriptionCosts[SubscriptionType.Personal], currency, 0.04m) },
                {"pro", CurrencyConversion.GetConvertedRate(SubscriptionsMeta.SubscriptionCosts[SubscriptionType.Pro], currency, 0.03m) },
                {"business", CurrencyConversion.GetConvertedRate(SubscriptionsMeta.SubscriptionCosts[SubscriptionType.Business], currency)  }
            }.ToString();
        }

        [HttpPost("transactions")]
        public ActionResult<string> GetTransactions([FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            return Db.WalletTransactionsCollection.All(x => x.SenderUsername == jwt.Username || x.ReceiverUsername == jwt.Username).ToJson();
        }
    }
}
