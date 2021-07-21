using Microsoft.AspNetCore.Mvc;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using static Newtonsoft.Json.JsonConvert;
using ServerLib;

namespace OTaff.Controllers
{
    [Route("api/support")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        [HttpPost("newticket")]
        public ActionResult<string> NewTicket([FromForm] string token, [FromForm] string jticket)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var ticket = jticket.ToObject<SupportTicket>();

            if (ticket.RequestTitle.Length < 8) return BadRequest("Title is too short");
            if (ticket.Message.Length < 20) return BadRequest("The body of the ticket is too short");

            ticket.UserId = jwt.UserId;
            ticket.DateSent = DateTime.UtcNow;

            Db.SupportTickets.InsertOne(ticket);

            return ticket.Id;
        }

        [HttpPost("sendmessage")]
        public ActionResult<string> NewMessage([FromForm] string token, [FromForm] string jmessage, [FromForm] string id)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var ticket = Db.SupportTickets.First(x => x.Id == id && x.UserId == jwt.UserId);
            if (ticket is null) return NotFound("This ticket does not exist");

            var message = jmessage.ToObject<TicketMessage>();
            message.Date = DateTime.UtcNow;
            message.TicketId = id;
            message.UserId = jwt.UserId;

            Db.TicketMessages.InsertOne(message);

            return message.ToJson();
        }

        [HttpPost("tickets")]
        public async Task<ActionResult<string>> MyTickets([FromForm] string token, [FromForm] string id = null)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            if (id is null)
            {
                var tickets = Db.SupportTickets.All(x => x.UserId == jwt.UserId);
                var ids = tickets.Select(g => g.Id);
                var messages = await Db.MessagesCollection.AllAsync(x => ids.Contains(x.Id));
                return new Dictionary<string, dynamic>
                {
                    {"tickets", tickets },
                    {"messages", messages }
                }.ToJson();
            }
            else
            {
                return new Dictionary<string, dynamic>
                {
                    {"ticket", await Db.SupportTickets.FirstAsync(x=>x.Id == id && x.UserId == jwt.UserId) },
                    {"messages", await Db.TicketMessages.AllAsync(x=>x.TicketId == id) }
                }.ToJson();
            }
        }

        [HttpPost("report")]
        public ActionResult<string> ReportPost(
            [FromForm] string token, 
            [FromForm] string jreport)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var report = DeserializeObject<Report>(jreport);

            if (Db.ReportsCollection.CountDocuments(x => x.UserId == jwt.UserId && x.Date >= DateTime.UtcNow - TimeSpan.FromDays(2)) > 2)
                return BadRequest("You have reported too many posts, please wait some time, if your query is urgent please contact support");
            
            if (string.IsNullOrWhiteSpace(report.TargetId) || string.IsNullOrWhiteSpace(report.TargetId))
                return BadRequest("BAD DATA");

            report.Date = DateTime.UtcNow;
            report.Id = null;

            Db.ReportsCollection.InsertOne(report);

            return StatusCode(200);
        }
    }
}
