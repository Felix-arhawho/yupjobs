using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/disputes")]
    [ApiController]
    public class DisputesController : ControllerBase
    {
        [HttpPost("get")]
        public ActionResult<string> Disputes([FromForm] string token, [FromForm] string id = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            if (id is null)
                return Db.Disputes.All(x => x.UserIds.Contains(new(jwt.UserId, jwt.Username))).ToJson();
            else
            {
                var d = Db.Disputes.First(x => x.Id == id && x.UserIds.Contains(new(jwt.UserId, jwt.Username)));
                if (d is null) return NotFound();
                return d.ToJson();
            }
        }

        //[HttpPost("conv")]
        //public ActionResult<string> Conversation([FromForm] string token, [FromForm] string id)
        //{
        //    var jwt = token.ToObject<Jwt>();
        //    if (!jwt.Verify()) return Unauthorized();

        //    return 
        //}
    }
}
