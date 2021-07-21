
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Models;

namespace OTaff.Controllers
{
    [Route("api/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public static ServerStatus Status = ServerStatus.Online;
        public static double ServerVersion = 0.22;
        public static double ClientVersion = 0.22;


        [HttpPost("status")]
        public ActionResult<string> GetServerStatus()
        {
            var status = $"Welcome to Otaff, todays date is {DateTime.UtcNow.ToString("g")}";
            return new Tuple<string, ServerStatus>(status, Status).ToJson();
        }

        [HttpPost("version")]
        public ActionResult<string> Version() => ClientVersion.ToString();

        [HttpPost("sessiondata")]
        public async Task<ActionResult<string>> GetSessionData([FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();
            
            var user = await jwt.GetUserAsync();

            var sub = await Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);
            var profile = await Db.ProfilesCollection.FirstAsync(x => x.Id == jwt.ProfileId);
            var connect = await Db.ConnectAccounts.FirstAsync(x => x.UserId == jwt.UserId);
            var notifs = await Db.NotificationsCollections.AllAsync(x => x.UserId == jwt.UserId);
            var wallets = await Db.UserWalletsCollection.AllAsync(x => x.UserId == jwt.UserId && !x.Hidden);
            var methods = await Db.PaymentMethodsCollection.AllAsync(x => x.UserId == jwt.UserId);
            var jobs = await Db.OngoingJobsCollection.AllAsync(x => x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId);

            var ret = new Dictionary<string, dynamic>() 
            {
                {"subscription", sub},
                {"profile", profile},
                {"connect", connect},
                {"notifications", notifs},
                {"wallets", wallets },
                {"currencies", CurrencyConversion.Rates },
                {"methods", methods},
                {"jobs", jobs },
                //{"conversations", await Db.ConversationsCollection.AllAsync(x=>x.Members.Count(g=>g.UserId==jwt.UserId)>0) }
            };

            user.HashedPassword = null;
            ret["user"] = user;

            return ret.ToJson();
        }

        [HttpPost("register/{origin}")]
        public ActionResult<string> Register(string origin = null)
        {
            if (origin is null) return Ok();
            else return Ok();
        }

        public enum ServerStatus
        {
            Online = 0,
            NoPayments = 1,
            Maintenance = 2,
            Broken = 3
        }
    }
}
