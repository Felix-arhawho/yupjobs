using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OTaff.Lib.Extensions;
using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace OTaff.Controllers
{
    [Route("api/info")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        [HttpPost("canpost/{id}")]
        public ActionResult<string> CanPost(string id)
        {
            var sub = Db.SubscriptionsCollection.FirstAsync(x => x.UserId == id);
            var searchCount = Db.JobSearchPostsCollection.CountDocumentsAsync(x => x.UserId == id && x.PostDate >= DateTime.UtcNow - TimeSpan.FromDays(7) && x.Active);
            var postCount = Db.JobPostsCollection.CountDocumentsAsync(x => x.UserId == id && x.PostDate >= DateTime.UtcNow - TimeSpan.FromDays(7) && x.Active);
            var appliesCount = Db.JobAppliesCollection.CountDocumentsAsync(x => x.UserId == id && x.Date >= DateTime.UtcNow-TimeSpan.FromDays(7));
            var proposeCount = Db.SearchProposalsCollection.CountDocumentsAsync(x => x.UserId == id && x.Date >= DateTime.UtcNow - TimeSpan.FromDays(7));

            return new JObject() 
            {
                {"canpost", postCount.Result < sub.Result.Limits.JobPostLimit },
                {"cansearch", searchCount.Result < sub.Result.Limits.JobSearchPostLimit },
                {"canapply", appliesCount.Result < sub.Result.Limits.WeeklyApplyLimit },
                {"canpropose", proposeCount.Result < sub.Result.Limits.WeeklyJobSearchProposalsLimit }
            }.ToString();
        }

        [HttpPost("tos")]
        public ActionResult<string> ToS() => System.IO.File.ReadAllText("txt/tos.txt");
    }
}
