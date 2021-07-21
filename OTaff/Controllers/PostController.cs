using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Lib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/post")] [ApiController]
    public class PostController : ControllerBase
    {
        [HttpPost("gethire")]
        public ActionResult<string> GetLook(
            //[FromForm] string token,
            //provide either a filter or an id, if both are provided it will look for id
            [FromForm] string jfilter = null,
            [FromForm] string id = null)
        {
            //var jwt = DeserializeObject<Jwt>(token);
            //if (!jwt.Verify()) return Unauthorized();

            try
            {
                if (id is null)
                {
                    var filter = jfilter is null
                        ? new JobPostFilter()
                        : DeserializeObject<JobPostFilter>(jfilter);

                    FilterDefinition<JobPost> dbfilter = SearchCtl.SearchFilter(filter);

                    return Db.JobPostsCollection.FindSync(dbfilter,
                        new FindOptions<JobPost, JobPost>()
                        {
                            Limit = filter.MaxCount,                            
                        }).ToList().ToJson();
                }
                else
                {
                    var post = Db.JobPostsCollection.Find(x => x.Id == id).FirstOrDefault();
                    if (post is null) return NotFound();

                    var upd = new UpdateDefinitionBuilder<JobPost>().Set(x => x.Views, post.Views + 1);
                    _ = Db.JobPostsCollection.UpdateOneAsync(x => x.Id == post.Id, upd);

                    return SerializeObject(post);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<JobPost>().ToJson();
            }
        }

        [HttpPost("applies/{id}")]
        public ActionResult<dynamic> GetApplies(string id) => Db.JobAppliesCollection.All(x => x.PostId == id).ToJson();


        [HttpPost("proposals/{id}")]
        public ActionResult<dynamic> GetProposals(string id) => Db.SearchProposalsCollection.All(x => x.PostId == id).ToJson();

        [HttpPost("newhire")]
        public async Task<ActionResult<string>> NewLook(
            [FromForm] string token,
            [FromForm] string jpost)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var sub = Db.SubscriptionsCollection.First(x => x.UserId == jwt.UserId);
            var limits = SubscriptionsMeta.Limits[sub.Type];
            var count = Db.JobPostsCollection.CountDocuments(x => x.UserId == jwt.UserId && x.Active);
            if (count >= limits.JobPostLimit) return BadRequest("You have too many active posts, you can upgrade your account to post more");

            var post = DeserializeObject<JobPost>(jpost);

            if (!post.VerifyContent(sub.Type)) return BadRequest("Please verify that each field is entered correctly");
            
            post.Active = true;
            post.UserId = jwt.UserId;
            post.MaxApplies = limits.JobPostApplicationLimit;

            Db.JobPostsCollection.InsertOne(post);
            return post.Id;
        }

        [HttpPost("edithire")]
        public ActionResult<string> EditLook(
            [FromForm] string token,
            [FromForm] string jpost)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var sub = Db.SubscriptionsCollection.First(x => x.UserId == jwt.UserId);
            var post = DeserializeObject<JobPost>(jpost);

            if (!post.VerifyContent(sub.Type)) return BadRequest("Please verify that each field is entered correctly");

            var dpost = Db.JobPostsCollection.First(x => x.Id == post.Id && x.UserId == jwt.UserId);
            if (dpost is null) return BadRequest("Post not found");

            dpost.Description = post.Description;
            dpost.Title = post.Description;
            dpost.Currency = post.Currency;
            dpost.Categories = post.Categories;
            dpost.ProposedSalary = post.ProposedSalary;
            dpost.Images = post.Images.Take(3).ToList();
            dpost.Tags = post.Tags.Take(sub.Limits.PostTagCount).ToList();
            dpost.WorkDuration = post.WorkDuration;
            dpost.MaxApplies = sub.Limits.JobPostApplicationLimit;

            Db.JobPostsCollection.ReplaceOne(x => x.Id == dpost.Id, dpost);

            return Ok();
        }

        [HttpPost("removehire")]
        public ActionResult<string> RemoveLook(
            [FromForm] string token,
            [FromForm] string id)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var post = Db.JobPostsCollection.Find(x => x.Id == id).FirstOrDefault();
            if (post is null) return BadRequest("Post does not exist");
            if (post.UserId != jwt.UserId) return BadRequest("You are not the owner of this post");

            Db.JobPostsCollection.UpdateOne(x => x.Id == id, new UpdateDefinitionBuilder<JobPost>().Set("Active", false));

            return Ok();
        }

        [HttpPost("getsearch")]
        public ActionResult<string> GetSearch(
            [FromForm] string id = null,
            [FromForm] string categories = null,
            [FromForm] string search = null,
            [FromForm] string tags = null,
            [FromForm] short maxCount = 40)
        {
            
            if (id is null)
            {
                var cats = categories.ToObject<List<JobCategory>>();
                if (string.IsNullOrWhiteSpace(search)) search = string.Empty;
                if (categories is null || cats.Count is 0 || cats[0] is JobCategory.Any)
                {
                    cats = new List<JobCategory>();
                    foreach (var i in Enum.GetValues(typeof(JobCategory))) cats.Add((JobCategory)i);
                }

                var tagsl = tags.ToObject<List<string>>();

                bool tagSearch = false;
                if (tagsl != null && tagsl.Count > 0) tagSearch = true;

                var maxDate = DateTime.UtcNow - TimeSpan.FromDays(61);
                //if (maxDate is null) maxDate = DateTime.UtcNow - TimeSpan.FromDays(61);

                if (tagSearch)
                    return Db.JobSearchPostsCollection.FindSync(x =>
                        (x.Title.ToLower().Contains(search.ToLower()) || x.Description.ToLower().Contains(search.ToLower()))
                        && tagsl.Any(g => x.Tags.Contains(g))
                        && cats.Any(g => x.Categories.Contains(g))
                        && x.PostDate >= maxDate,
                        new FindOptions<JobSearchPost, JobSearchPost>()
                        {
                            Limit = maxCount,
                            Sort = new SortDefinitionBuilder<JobSearchPost>().Descending(x => x.PostDate),
                        }).ToList().ToJson();
                
                else return Db.JobSearchPostsCollection.FindSync(x =>
                        (x.Title.ToLower().Contains(search.ToLower()) || x.Description.ToLower().Contains(search.ToLower()))
                        && cats.Any(g => x.Categories.Contains(g))
                        && x.PostDate >= maxDate,
                        new FindOptions<JobSearchPost, JobSearchPost>()
                        {
                            Limit = maxCount,
                            Sort = new SortDefinitionBuilder<JobSearchPost>().Descending(x => x.PostDate),
                        }).ToList().ToJson();
            }
            else
            {
                var ns = Db.JobSearchPostsCollection.First(x => x.Id == id);
                return ns.ToJson();
            }
        }

        [HttpPost("newsearch")]
        public async Task<ActionResult<string>> NewSearch(
            [FromForm] string token,
            [FromForm] string jpost)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var user = await jwt.GetUserAsync();
            var post = jpost.ToObject<JobSearchPost>();

            var count = await Db.JobSearchPostsCollection.CountDocumentsAsync(x => x.UserId == jwt.UserId && x.Active);

            var sub = Db.SubscriptionsCollection.Find(x => x.UserId == jwt.UserId).FirstOrDefault();
            var limits = SubscriptionsMeta.Limits[sub.Type];
            
            if (count >= limits.JobSearchPostLimit) 
                return BadRequest("You have too many active posts, you can upgrade your account to post more");

            //var jwt2 = DeserializeObject<Jwt>(token);
            
            if (!post.VerifyContent()) 
                return BadRequest("Please verify that each field is entered correctly");

            post.Active = true;
            post.Id = null;
            
            
            post.PostDate = DateTime.UtcNow;
            post.UserId = user.Id;
            post.Username = user.Username;
            post.MaxApplies = limits.JobPostApplicationLimit;
            
            Db.JobSearchPostsCollection.InsertOne(post);

            return post.Id;
        }

        [HttpPost("editsearch")]
        public async Task<ActionResult<string>> EditSearch(
            [FromForm] string token,
            [FromForm] string jpost)
        {
            var jwt = token.ToObject<Jwt>();
            var post = jpost.ToObject<JobSearchPost>();

            var sub = await Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);
            if (!jwt.Verify()) return Unauthorized();
            //if (!post.VerifyContent(sub.Type)) return BadRequest("Please verify that each field is entered correctly");

            var dpost = Db.JobSearchPostsCollection.Find(x => x.Id == post.Id).FirstOrDefault();
            if (dpost is null) return BadRequest("Post not found");
            if (dpost.UserId != jwt.UserId) return BadRequest("You are not the owner of this post");

            //var id = dpost.Id;
            //dpost.Id = null;
            dpost.Categories = post.Categories;
            dpost.Description = post.Description;
            dpost.Title = post.Title;
            dpost.Tags = post.Tags.Take(sub.Limits.PostTagCount).ToList();
            dpost.Currency = post.Currency;
            dpost.RequestedSalary = post.RequestedSalary;
            dpost.Active = post.Active;

            Db.JobSearchPostsCollection.ReplaceOne(x => x.Id == dpost.Id, dpost);

            return Ok();
        }

        [HttpPost("removesearch")]
        public ActionResult<string> RemoveSearch(
            [FromForm] string token, 
            [FromForm] string id)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var post = Db.JobSearchPostsCollection.Find(x => x.Id == id).FirstOrDefault();
            if (post is null) return BadRequest("Post does not exist");
            if (post.UserId != jwt.UserId) return BadRequest("You are not the owner of this post");

            Db.JobSearchPostsCollection.UpdateOne(x => x.Id == id, new UpdateDefinitionBuilder<JobSearchPost>().Set("Active", false));

            return Ok();
        }

        [HttpPost("mine")]
        public ActionResult<string> Mine([FromForm] string token)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();
            
            var posts1 = Db.JobPostsCollection.AllAsync(x => x.UserId == jwt.UserId);
            var posts2 = Db.JobSearchPostsCollection.AllAsync(x => x.UserId == jwt.UserId);

            return new Dictionary<string, object>()
            {
                {"jobposts", posts1.Result },
                {"searchposts", posts2.Result }
            }.ToJson();
        }

        static Random Rnd = new Random();
        [HttpPost("randompromo/{count}")]
        public ActionResult<string> RandomPromoPost(short count = 2, JobCategory cat = JobCategory.Any)
        {
            var posts = Db.JobPostsCollection.All(x => 
                x.Promoted 
                && x.Categories.Contains(cat) 
                && x.PostDate > DateTime.UtcNow - TimeSpan.FromDays(26));
            var selected = new List<JobPost>();
            for (int i = 0; i < count; i++)
            {
                selected.Add(posts[Rnd.Next(0, posts.Count)]);
            }
            return selected.ToJson();
        }
    }
}
