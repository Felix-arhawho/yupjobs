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
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        [HttpPost("post")]
        public ActionResult<string> PostReview(
            [FromForm] string token, 
            [FromForm] string jreview, 
            [FromForm] string jobId, 
            [FromForm] string profileId)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var review = jreview.ToObject<ProfileReview>();

            ExpressionFilterDefinition<ProfileReview> filter;
            filter = new ExpressionFilterDefinition<ProfileReview>((ProfileReview x) => x.JobId == jobId && x.Type == review.Type);
            if (Db.ReviewsCollections.CountDocuments(filter) > 0)
                return BadRequest("You have already posted a review");

            review.Id = null;
            if (!string.IsNullOrWhiteSpace(review.ReviewContent))
                review.ReviewContent = new string(review.ReviewContent.Take(600).ToArray());

            var profile = Db.ProfilesCollection.FirstAsync(x => x.Id == profileId);
            var job = Db.OngoingJobsCollection.First(x => x.Id == jobId);

            if (job is null || !(jwt.UserId == job.EmployeeId || jwt.UserId == job.EmployerId))
                return BadRequest("Job does not exist");

            if (!(review.Type is ReviewType.Employee
                ? (job.EmployeeId == profile.Result.UserId)
                : (job.EmployerId == profile.Result.UserId)))
                return BadRequest("The job does not include this person");

            review.UserId = jwt.UserId;
            review.Username = jwt.Username;
            review.DatePosted = DateTime.UtcNow;
            review.TargetProfileId = profileId;
            review.Hidden = false;

            Db.ReviewsCollections.InsertOne(review);

            return Ok();
        }

        [HttpPost("getprofile/{id}")]
        public ActionResult<string> GetReviews(string id)
        {
            var reviews = Db.ReviewsCollections.Find(x => x.TargetProfileId == id).ToList();
            return reviews.ToJson();
        }

        [HttpPost("get/job/{Id}")]
        public ActionResult<string> GetReview(string id)
        {
            var review = Db.ReviewsCollections.Find(x => x.JobId == id).FirstOrDefault();
            if (review is null) return NotFound();
            return review.ToJson();
        }

        [HttpPost("delete/{id}")]
        public ActionResult<string> Delete([FromForm] string token, string id)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();
            Db.ReviewsCollections.DeleteOne(x => x.Id == id && jwt.UserId == jwt.Id);
            return Ok();
        }

        //[HttpPost("g")]
        //[HttpPost("")]
        //public ActionResult<string> Get
    }
}
