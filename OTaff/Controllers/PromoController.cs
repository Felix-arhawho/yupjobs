using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Models;

namespace OTaff.Controllers
{
    [Route("api/promo")]
    [ApiController]
    public class PromoController : ControllerBase
    {
        public static decimal EurPromoCost = 2.99m;

        [HttpPost("promote")]
        public ActionResult<string> Promote(
            [FromForm] string token,
            [FromForm] string methodId,
            [FromForm] string postId,
            [FromForm] Currency currency = Currency.EUR,
            [FromForm] bool isSearch = false)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var method = Db.PaymentMethodsCollection.First(x => x.Id == methodId && x.UserId == jwt.UserId);
            if (method is null) return NotFound("Please select a valid payment method");
            var post = Db.JobPostsCollection.First(x => x.Id == postId && x.UserId == jwt.UserId);
            if (post is null) return NotFound("Post doesn't exist");

            //var charge = ChargeCtl.


            return default;
        }



        static Random Rnd = new Random();
        
        [HttpPost("getjob/{cat}")]
        public ActionResult<string> GetOne(JobCategory cat)
        {
            var posts = Db.JobPostsCollection.Find(x =>
                x.Promoted
                && x.Categories.Contains(cat)
                && x.Active
                && x.PostDate > DateTime.UtcNow - TimeSpan.FromDays(15)).ToList();
            var selected = posts[Rnd.Next(0, posts.Count)];
            return selected.ToJson();
        }

        [HttpPost("getsearch/{cat}")]
        public ActionResult<string> GetSearch(JobCategory cat)
        {
            var posts = Db.JobSearchPostsCollection.Find(x =>
                x.Promoted && x.Categories.Contains(cat) && x.PostDate > DateTime.UtcNow-TimeSpan.FromDays(15)).ToList();

            return default;
        }

    }
}
