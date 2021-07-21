using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib;
using ServerLib;
using Stripe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/webhooks")]
    public class WebHooks : ControllerBase
    {
        [HttpPost("stripe")]
        public IActionResult Index()
        {
            using var sr = new StreamReader(HttpContext.Request.Body);
            var json = sr.ReadToEndAsync().Result;

            var stripeEvent = EventUtility.ParseEvent(json);

            _ = Task.Run(delegate
            {

                // Handle the event
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var bill = Db.UserBillsCollection.Find(x => x.StripeIntentId == paymentIntent.Id).FirstOrDefault();
                    //bill

                }
                else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var bill = Db.UserBillsCollection.Find(x => x.StripeIntentId == paymentIntent.Id).FirstOrDefault();

                }
                else if (stripeEvent.Type is Events.ChargeDisputeCreated)
                {

                }

            });

            return Ok();
        }
    }
}
