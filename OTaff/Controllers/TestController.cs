//using Microsoft.AspNetCore.Mvc;
//using OTaff.Lib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using MongoDB.Driver;
//using System.Threading.Tasks;
//using SharedLib.Models;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;
//using OTaff.Lib.Extensions;
//using OTaff.Lib.Money;
//using ServerLib;

//namespace OTaff.Controllers
//{
//    [Route("api/test")]
//    [ApiController]
//    public class TestController : ControllerBase
//    {
//        [HttpPost("test")]
//        public ActionResult<string> Test()
//        {
//            return Guid.NewGuid().ToString();
//            //return JsonConvert.SerializeObject(HttpContext.Request.Ho, Formatting.Indented);
//        }

//        [HttpPost("invoice")]
//        public ActionResult<string> Invoice([FromForm] string username)
//        {
//            var user = Db.UsersCollection.First(x => x.Username == username);
//            var method = Db.PaymentMethodsCollection.First(x => x.UserId == user.Id && x.Type == MethodType.StripeInvoice);

//            var ret = StripeController.CreateLinkInvoice(user, DateTime.UtcNow + TimeSpan.FromDays(10), Currency.EUR, 50, "Test invoice charge");

//            return ret.ToJson();
//        }

//        [HttpPost("sepa")]
//        public ActionResult<string> Sepa([FromForm] string username, [FromForm] string methodId)
//        {
//            var method = Db.PaymentMethodsCollection.First(x => x.Id == methodId);
//            var user = Db.UsersCollection.First(x => x.Username == username);

//            var charge = StripeController.ChargeSepa(user, 10000, new decimal[]{ 0,0,0}, Currency.EUR, "Test sepa charge", method, true).Result;

//            return charge.ToJson();
//        }

//        [HttpGet("intent/{id}")]
//        public ActionResult<string> GetIntent(string id)
//        {
//            return StripeController.IntentServiceEU.Get(id).ToJson();
//        }

//        [HttpPost("sofort")]
//        public ActionResult<string> Sofort([FromForm] string username)
//        {
//            var user = Db.UsersCollection.Find(x => x.Username == username).FirstOrDefault();
//            StripeController.CreateSofortMethod(user).Wait();

//            var method = Db.PaymentMethodsCollection.First(x => x.UserId==user.Id && x.Type == MethodType.Sofort);

//            var charge = StripeController.ChargeSofort(user, 10000, Currency.EUR, "TEST SOFORT", method);
            
//            return charge.ToJson();
//        }

//        [HttpPost("charge")]
//        public ActionResult<string> Charge([FromForm] string username, [FromForm] string password, [FromForm] string methodId)
//        {
//            var method = Db.PaymentMethodsCollection.Find(x => x.Id == methodId).FirstOrDefaultAsync();
//            var user = Db.UsersCollection.Find(x=>x.Username == username).FirstOrDefault();
            
//            if (user is null) {
//                return BadRequest("User is null");
//            }
//            var charge = StripeController.ChargeCustomerOnSession(user, Currency.EUR, "TEST", method.Result, 1000).Result;

//            return JsonConvert.SerializeObject(charge, Formatting.Indented).ToJson();
//        }

//        [HttpPost("oncharge")]
//        public ActionResult<string> OnCharge([FromForm] string username, [FromForm] string password, [FromForm] string methodId)
//        {
//            var method = Db.PaymentMethodsCollection.Find(x => x.Id == methodId).FirstOrDefaultAsync();
//            var user = Db.UsersCollection.Find(x => x.Username == username).FirstOrDefault();
//            if (user is null) return BadRequest("User is null");
//            var charge = StripeController.ChargeCustomerOnSession(user, Currency.EUR, "TEST", method.Result, 1000).Result;

//            return JsonConvert.SerializeObject(charge, Formatting.Indented);
//        }

//    }
//}
