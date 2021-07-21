using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using ServerLib.Models;
using SharedLib.Lib;
using SharedLib.Models;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/subs")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="type"></param>
        /// <param name="methodId"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        [HttpPost("subscribe")]
        public async Task<ActionResult<string>> Subscribe(
            [FromForm] string token,
            [FromForm] SubscriptionType type,
            [FromForm] Currency currency = Currency.EUR,
            [FromForm] string methodId = null,
            [FromForm] bool convert = true)
        {
            ///// if action pending skip
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            
            var user = await jwt.GetUserAsync();
            var method = methodId is null ? null : await Db.PaymentMethodsCollection.FirstAsync(x => x.Id == methodId && jwt.UserId == x.UserId);
            var sub = Db.SubscriptionsCollection.First(x => x.UserId == jwt.UserId);


            if (type is SubscriptionType.Free) 
            {
                Db.SubscriptionsCollection.UpdateOne(x => x.Id == sub.Id, new UpdateDefinitionBuilder<UserSubscription>()
                    .Set(x => x.NextType, SubscriptionType.Free)
                    .Set(x => x.Renew, false)
                    .Set(x => x.PaymentMethodId, null));

                return Ok();
            };

            if (sub.NextType == type) return BadRequest("You already have this subscription");

            var allowedMethods = new MethodType[]
            {
                MethodType.CreditCard,
                MethodType.SepaDirect,
                //MethodType.BecsDirect,
            };
            //if (method is null) return BadRequest("You need to provide a payment method to subscribe");
            //if (!allowedMethods.Contains(method.Type)) return BadRequest("You need to select a valid payment method for subscriptions, otherwise you can prepay your subscription using any method");

            //if (methodId is null) ;
            if (convert)
            {
                var s = await Db.Client.StartSessionAsync();
                s.StartTransaction();
                var remaining = sub.ValidUntil > DateTime.UtcNow
                    ? sub.ValidUntil - DateTime.UtcNow
                    : TimeSpan.Zero;
                    
                var newTime = SubConvert.ConvertSub(remaining, sub.Type, type);
                var newDate = DateTime.UtcNow + newTime;

                Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id,
                    new UpdateDefinitionBuilder<UserSubscription>()
                        .Set(x => x.Type, type)
                        .Set(x => x.NextType, type)
                        .Set(x => x.ValidUntil, newDate)
                        .Set(x => x.Renew, true)
                        .Set(x => x.PaymentMethodId, method.Id));
                s.CommitTransaction();
                return Ok();
            }
            else
            {
                if (sub is null)
                {
                    var action = new SubscriptionActionData()
                    {
                        Months = 1,
                        ActionType = BillAction.Subscription,
                        Executed = false,
                        Issued = DateTime.UtcNow,
                        SubId = sub.Id,
                        UserId = user.Id,
                        Type = type,
                        Description = $"{type.GetName()} subscription of {1} months",
                    };
                    var cost = Ez.CalculateSubDiscount(type, 1).Item3;
                    var charge = ChargeCtl.ChargeMethod(
                        user,
                        currency,
                        method,
                        new[] { cost, cost, cost},
                        desc: $"{type.GetName()} subscription of {1} months",
                        action: action);
                    


                    return charge.ToJson();
                }
                else
                {
                    var upd = new UpdateDefinitionBuilder<UserSubscription>().Set(x=>x.NextType, type).Set(x=>x.PaymentMethodId, methodId).Set(x=>x.Renew, true);
                    Db.SubscriptionsCollection.UpdateOne(x => x.Id == sub.Id, upd);
                    return Ok();
                }
            }



            //if (!convert)
            //    Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id, 
            //        new UpdateDefinitionBuilder<UserSubscription>()
            //               .Set(x => x.NextType, type)
            //               .Set(x => x.Renew, true)
            //               .Set(x => x.PaymentMethodId, method.Id));
            //else
            //{
            //    var remaining = sub.ValidUntil > DateTime.UtcNow 
            //        ? sub.ValidUntil - DateTime.UtcNow 
            //        : TimeSpan.Zero;

            //    var newTime = SubConvert.ConvertSub(remaining, sub.Type, type);
            //    var newDate = DateTime.UtcNow + newTime;

            //    Db.SubscriptionsCollection.UpdateOne(s, x => x.Id == sub.Id, 
            //        new UpdateDefinitionBuilder<UserSubscription>()
            //            .Set(x => x.Type, type)
            //            .Set(x => x.NextType, type)
            //            .Set(x => x.ValidUntil, newDate)
            //            .Set(x => x.Renew, true)
            //            .Set(x => x.PaymentMethodId, method.Id));
            //}


            return Ok();
        }

        [HttpPost("buymonths")]
        public async Task<ActionResult<string>> BuyMonths(
            [FromForm] string token,
            [FromForm] Currency currency,
            [FromForm] string methodId,
            [FromForm] short months,
            [FromForm] SubscriptionType type)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            
            var sub = await Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);
            var method = await Db.PaymentMethodsCollection.FirstAsync(x => x.Id == methodId);
            var user = await jwt.GetUserAsync();

            var cost =
                SubscriptionsMeta.SubscriptionCosts[type] 
                + ((decimal)Ez.CalculateSubDiscount(type, months).Item2 * SubscriptionsMeta.SubscriptionCosts[type]);


            if (user is null || method is null || user.Id != method.UserId) 
                return BadRequest("Method does not exist");
            
            var action = new SubscriptionActionData()
            {
                Months = months,
                ActionType = BillAction.Subscription,
                Executed = false,
                Issued = DateTime.UtcNow,
                SubId = sub.Id,
                UserId = user.Id,
                Type = type,
                Description = $"{type.GetName()} subscription of {months} months",
            };

            var desc = "Bought prepaid subscription from YupJobs";

            var discount = Ez.CalculateSubDiscount(type, months);
            var ret = ChargeCtl.ChargeMethod(
                user,
                currency,
                method,
                new[] { discount.Item3, discount.Item3, discount.Item3},
                $"Prepaid {type.GetName()} plan of {months} months",
                action: action);

            return ret.ToJson();
        }
    }
}
