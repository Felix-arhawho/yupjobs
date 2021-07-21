using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib.Money;
using OTaff.Lib.Extensions;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OTaff.Lib;
using ServerLib;

namespace OTaff.Controllers
{
    [Route("api/billing")]
    [ApiController]
    public class BillingController : ControllerBase
    {
        [HttpPost("currencyrates")]
        public ActionResult<string> GetCurrencyRates([FromForm] string apikey = "apikey")
        {
            return CurrencyConversion.Rates.ToJson();
        }

        [HttpPost("get")]
        public ActionResult<string> GetBills([FromForm] string token, [FromForm] string id = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            if (id is null) return Db.UserBillsCollection.All(x => x.UserId == jwt.UserId).ToJson();
            else return Db.UserBillsCollection.First(x => x.Id==id && x.UserId == jwt.UserId).ToJson();
        }

        [HttpPost("pay")]
        public ActionResult<string> PayBill([FromForm] string token, [FromForm] string id, [FromForm] string methodId)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var bill = Db.UserBillsCollection.FirstAsync(x => x.Id == id && x.UserId == jwt.UserId);
            var user = jwt.GetUserAsync();
            var method = Db.PaymentMethodsCollection.FirstAsync(x => x.Id == methodId && x.UserId == jwt.UserId);

            var ret = ChargeCtl.ChargeMethod(
                user.Result, 
                bill.Result.Currency, 
                method.Result, 
                new[] { 0m, bill.Result.TotalAmountRequested, 0m }, 
                bill.Result.Description, 
                null, 
                bill: bill.Result,
                true,
                false);

            return ret.ToJson();
        }

        //[HttpPost("")]
        //public ActionResult<string> 

        [HttpPost("confirmbill")]
        public ActionResult<string> ConfirmBill([FromForm] string token, [FromForm] string billId)
        {
            try
            {
                var jwt = token.ToObject<Jwt>();
                if (!jwt.Verify()) return Unauthorized();

                var bill = Db.UserBillsCollection.Find(x => x.Id == billId).FirstOrDefault();
                if (bill is null || bill.UserId != jwt.UserId) return NotFound("Bill does not exist");
                if (bill.Paid) return new Dictionary<string, dynamic>
                {
                    {"paid", true },
                    {"total", bill.TotalAmountRequested },
                }.ToJson();

                PaymentIntent intent;
                lock (StripeController.InvoiceLock) intent = StripeController.IntentServiceEU.Capture(bill.StripeIntentId);

                if (intent.Status is "succeeded")
                {
                    var upd = new UpdateDefinitionBuilder<UserBill>()
                        .Set(x => x.Paid, true)
                        .Set(x => x.PaidOn, DateTime.UtcNow)
                        .Set(x => x.Status, SPaymentStatus.Success)
                        .Set(x => x.TotalAmountAllocated, intent.AmountReceived.GetMoney(bill.Currency))
                        .Set(x => x.ShouldAct, bill.Action != BillAction.None)
                        .Set(x => x.Captured, true);
                    Db.UserBillsCollection.UpdateOne(x => x.Id == billId, upd);

                    //Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, upd
                    //    .Set(x => x.Paid, intent.Status is "succeeded")
                    //    .Set(x => x.PaidOn, DateTime.UtcNow)
                    //    .Set(x => x.Status, SPaymentStatus.Success)
                    //    .Set(x => x.Captured, true)
                    //    .Set(x => x.TotalAmountAllocated, intent.AmountReceived.GetMoney(currency)));

                    return new Dictionary<string, dynamic>()
                    {
                        {"delayed", false },
                        {"paid", true },
                        {"total", bill.TotalAmountRequested },
                        {"currency", Enum.GetName(typeof(Currency), bill.Currency) }
                    }.ToJson();
                }
                else if (intent.Status is "processing")
                {
                    var upd = new UpdateDefinitionBuilder<UserBill>()
                        .Set(x => x.Paid, false)
                        .Set(x => x.Status, SPaymentStatus.Pending)
                        .Set(x => x.ShouldAct, false)
                        .Set(x => x.Captured, true);
                    Db.UserBillsCollection.UpdateOne(x => x.Id == billId, upd);
                    return new Dictionary<string, dynamic>() {
                        {"delayed", true },
                        {"paid", false }
                    }.ToString();
                }
                else
                {
                    var upd = new UpdateDefinitionBuilder<UserBill>()
                        .Set(x => x.Paid, false)
                        .Set(x => x.NextVerif, DateTime.MaxValue)
                        .Set(x => x.Status, SPaymentStatus.Fail)
                        .Set(x => x.ShouldAct, false);
                    Db.UserBillsCollection.UpdateOne(x => x.Id == billId, upd);

                    return BadRequest("Capture has failed");
                }
            }
            catch 
            {
                return BadRequest("Paid");
            }
        }
    }
}
