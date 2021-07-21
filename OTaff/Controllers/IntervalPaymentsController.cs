using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/interval")]
    [ApiController]
    public class IntervalPaymentsController : ControllerBase
    {
        [HttpPost("start")]
        public ActionResult<string> Start([FromForm] string token,
            [FromForm] decimal amount,
            [FromForm] Currency currency,
            [FromForm] string jobId,
            [FromForm] bool instant,
            [FromForm] TimeSpan interval,
            [FromForm] string methodId,
            [FromForm] string title)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var job = Db.OngoingJobsCollection.First(x => x.Id == jobId);
            if (job is null) return NotFound("Job not found");
            if (interval > TimeSpan.FromDays(60)) return BadRequest("Interval cannot exceed 60 days");

            var order = new IntervalPaymentOrder()
            {
                Active = true,
                Currency = currency,
                EmployeeId = job.EmployeeId,
                EmployerId = job.EmployerId,
                EmployerUsername = jwt.Username,
                JobId = job.Id,
                PaymentInterval = interval,
                LastPayment = DateTime.UtcNow,
                NextPayment = instant ? DateTime.UtcNow : DateTime.UtcNow+interval,
                Payment = amount,
                PaymentMethodId = methodId,
                StartedOn = DateTime.UtcNow,
                Title = new string(title.Take(60).ToArray())
            };

            Db.IntervalPaymentOrders.InsertOne(order);
            return order.Id;
        }

        [HttpPost("edit")]
        public ActionResult<string> Update([FromForm] string token, [FromForm] string id, [FromForm] TimeSpan interval, [FromForm] decimal amount, [FromForm] string title, [FromForm] Currency currency)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var order = Db.IntervalPaymentOrders.First(x => x.Id == id && (x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId));
            if (order is null) return NotFound("This order doesn't exist");
            var upd = new UpdateDefinitionBuilder<IntervalPaymentOrder>()
                .Set(x=>x.PaymentInterval, interval)
                .Set(x=>x.NextPayment, order.LastPayment+interval)
                .Set(x=>x.Payment, amount)
                .Set(x=>x.Title, title);

            Db.IntervalPaymentOrders.UpdateOne(x => x.Id == id, upd);
            return Db.IntervalPaymentOrders.First(x=>x.Id==id).ToJson();
        }

        [HttpPost("get")]
        public ActionResult<string> Get([FromForm] string token, [FromForm] string id = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            if (id is null)
            {
                return Db.IntervalPaymentOrders.All(x => x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId).ToJson();
            }
            else
            {
                return Db.IntervalPaymentOrders.First(x => x.Id == id && (x.EmployerId==jwt.UserId||x.EmployeeId==jwt.UserId)).ToJson();
            }
        }

        [HttpPost("stop")]
        public ActionResult<string> Stop([FromForm] string token,
            [FromForm] string id,
            [FromForm] string jobId)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();
            Db.IntervalPaymentOrders.UpdateOne(x => x.Id == id && x.JobId == jobId, new UpdateDefinitionBuilder<IntervalPaymentOrder>()
                .Set(x => x.Active, false));
            return Ok();
        }

    }
}
