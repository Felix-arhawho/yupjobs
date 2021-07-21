using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        [HttpPost("seen")]
        public void SetSeen(
            [FromForm] string token,
            [FromForm] string id)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return;

            Db.NotificationsCollections.UpdateOne(x => x.Id == id && x.UserId == jwt.UserId, new UpdateDefinitionBuilder<Notification>().Set(x => x.Seen, true));
        }

        [HttpPost("clicked")]
        public void SetClicked(
            [FromForm] string token,
            [FromForm] string id)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return;

            Db.NotificationsCollections.UpdateOne(x => x.Id == id && x.UserId == jwt.UserId, new UpdateDefinitionBuilder<Notification>().Set(x => x.Clicked, true));
        }

        [HttpPost("get")]
        public ActionResult<string> Get(
            [FromForm] string token
            /*[FromForm] DateTime? since = null*/)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var since = DateTime.UtcNow - TimeSpan.FromDays(360);

            return Db.NotificationsCollections.All(x => x.UserId == jwt.UserId && x.Date > since).ToJson();
        }

        [HttpPost("new")]
        public ActionResult<string> New(
            [FromForm] string token, [FromForm] string jnotif)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var notif = jnotif.ToObject<Notification>();
            notif.Clicked = false;
            notif.Seen = false;
            notif.UserId = jwt.UserId;
            notif.Id = null;

            Db.NotificationsCollections.InsertOne(notif);
            return notif.ToJson();
        }

        //[HttpPost("htt")]
    }
}
