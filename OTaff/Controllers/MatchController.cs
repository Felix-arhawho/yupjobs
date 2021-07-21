using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.String;
using static Newtonsoft.Json.JsonConvert;
using ServerLib;

namespace OTaff.Controllers
{
    [Route("api/match")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        //[HttpPost("view/{Id}/{Type}")]
        //public ActionResult<string> Viewed(string id, string type)
        //{
        //    if (type is "jobpost")
        //    {
        //        var post =
        //    }
        //    else if (type is "searchpost")
        //    {

        //    }

        //}

        [HttpPost("apply")]
        public async Task<ActionResult<string>> Apply(
            [FromForm] string token,
            [FromForm] string japplication)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var sub = await Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);
            var application = japplication.ToObject<JobApply>();

            if (application.PostId is null) 
                return BadRequest("Post ID is null");
            if (Db.JobAppliesCollection.CountDocuments(x => x.UserId == jwt.UserId && x.Date > DateTime.UtcNow - TimeSpan.FromDays(7)) 
                >= sub.Limits.WeeklyApplyLimit)
                return BadRequest("You have made too many posts, please upgrade your subscription or wait");
            if (IsNullOrWhiteSpace(application.Message) || application.Message.Length < 120) 
                return BadRequest("The application must be more than 120 characters");

            var applies = Db.JobAppliesCollection.CountDocumentsAsync(x => x.PostId == application.PostId);
            var post = Db.JobPostsCollection.Find(x => x.Id == application.PostId).FirstOrDefault();
            if (applies.Result >= post.MaxApplies) return BadRequest("Post has too many applications");

            application.Id = null;
            application.ProfileId = jwt.ProfileId;
            application.UserId = jwt.UserId;
            application.Username = jwt.Username;
            application.Date = DateTime.UtcNow;
            application.Hidden = false;

            Db.JobAppliesCollection.InsertOne(application);

            return application.Id;
        }

        [HttpPost("removeapply/{id}")]
        public ActionResult<string> RemoveApply(
            [FromForm] string token,
            string id)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var apply = Db.JobAppliesCollection.Find(x => x.Id == id).FirstOrDefault();
            if (apply is null || apply.UserId != jwt.UserId) return BadRequest("Post does not exist");
            Db.JobAppliesCollection.DeleteOne(x => x.Id == id && x.UserId == jwt.UserId);
            //Db.JobAppliesCollection.UpdateOne(x=>x.Id==id, new UpdateDefinitionBuilder<JobApply>().Set("Hidden", true));
            return Ok();
        }

        [HttpPost("propose")]
        public ActionResult<string> Propose(
            [FromForm] string token,
            [FromForm] string jproposal)
        {
            var proposal = jproposal.ToObject<SearchProposal>();
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var sub = Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);
            if (proposal.PostId is null) 
                return BadRequest("Post ID is null");
            if (IsNullOrWhiteSpace(proposal.Message) || proposal.Message.Length < 50)
                return BadRequest("The application must be more than 120 characters");

            if (Db.SearchProposalsCollection.CountDocuments(x => x.UserId == jwt.UserId && x.Date > DateTime.UtcNow - TimeSpan.FromDays(7)) 
                >= sub.Result.Limits.WeeklyJobSearchProposalsLimit)
                return BadRequest("You have proposed too many times, please wait or upgrade subscription");
            
            var applies = Db.SearchProposalsCollection.CountDocumentsAsync(x => x.PostId == proposal.PostId);
            var post = Db.JobSearchPostsCollection.Find(x => x.Id == proposal.PostId).FirstOrDefault();
            if (applies.Result >= post.MaxApplies) return BadRequest("Post has too many applications");

            proposal.Id = null;
            proposal.UserId = jwt.UserId;
            proposal.Username = jwt.Username;
            proposal.Date = DateTime.UtcNow;

            //if (proposal.ProposedSalary.Length != 2) proposal.ProposedSalary = post.RequestedSalary;
            Db.SearchProposalsCollection.InsertOne(proposal);

            return proposal.Id;
        }

        [HttpPost("getproposals/post/{id}")]
        public ActionResult<string> GetProposals(string id)
        {
            return Db.SearchProposalsCollection.All(x => x.PostId == id).ToJson();
        }

        [HttpPost("accept")]
        public ActionResult<string> AcceptProposal([FromForm] string token, [FromForm] string propId, [FromForm] string postId)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var proposal = Db.SearchProposalsCollection.First(x => x.Id == propId);
            var post = Db.JobSearchPostsCollection.First(x => x.Id == postId);
            if (proposal is null || post.UserId != jwt.UserId) return NotFound();
            using var s = Db.Client.StartSession();
            s.StartTransaction();

            var conv = new Conversation()
            {
                Created = DateTime.UtcNow,
                Members = new List<ConversationMember>
                {
                    new ConversationMember
                    {
                        UserId = post.UserId,
                        Username = post.Username
                    }, 
                    new ConversationMember
                    {
                        UserId = jwt.UserId,
                        Username = jwt.Username
                    }
                },
                Title = $"Job: {post.Title}"
            };

            Db.ConversationsCollection.InsertOne(s, conv);

            var job = new Job
            {
                EmployeeId = post.UserId,
                EmployerId = proposal.UserId,
                Currency = post.Currency,
                Status = JobStatus.Started,
                OriginalSearch = post,
                JobTitle = post.Title,
                Payment = post.RequestedSalary[0],
                IsPaidOnPlatform = true,                
                ConversationId = conv.Id
            };
            Db.OngoingJobsCollection.InsertOne(s, job);
            Db.NotificationsCollections.InsertOne(s, new Notification
            {
                UserId = job.EmployerId,
                Date = DateTime.UtcNow,
                Description = $"Job: {job.JobTitle} has started",
                Title = "Job started",
                Href = $"/job/ongoing/{job.Id}"
            });

            s.CommitTransaction();
            return job.Id;
        }

        [HttpPost("removepropose/{id}")]
        public ActionResult<string> RemovePropose(
            [FromForm] string token,
            string id)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            var proposal = Db.SearchProposalsCollection.Find(x => x.Id == id).FirstOrDefault();
            if (proposal is null || proposal.UserId != jwt.UserId) return BadRequest("Post does not exist");
            Db.SearchProposalsCollection.UpdateOne(x => x.Id == id, new UpdateDefinitionBuilder<SearchProposal>().Set("Hidden", true));
            return StatusCode(200);
        }
    }
}
