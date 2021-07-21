using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Models;
using OTaff.Lib.Money;
using ServerLib;
using ServerLib.Models;
using SharedLib.Models;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpPost("set2fa/{val}")]
        public ActionResult<string> Set2FA([FromForm] string token, bool val)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            Db.UsersCollection.UpdateOne(x=>x.Id==jwt.UserId, new UpdateDefinitionBuilder<User>()
                .Set(x=>x.TwoFactorAuth, val));

            return Ok();
        }

        [HttpPost("updateaccount")]
        public ActionResult<string> UpdateAccount(
            [FromForm] string token,
            [FromForm] string jprofile,
            [FromForm] string juser)
        {
            //TODO verify information
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var user = juser.ToObject<User>();
            var profile = jprofile.ToObject<Profile>();


            var dbUser = Db.UsersCollection.FirstAsync(x => x.Id == jwt.UserId);
            var dbProfile = Db.ProfilesCollection.FirstAsync(x => x.Id == jwt.ProfileId);

            if (Db.UsersCollection.CountDocuments(x => x.Email == profile.Email) > 0)
                return BadRequest("Email is already used in another account");
            
            if (string.IsNullOrWhiteSpace(profile.Email)) profile.Email = dbUser.Result.Email;
            _ = Task.Run(delegate 
            {
                dbProfile.Result.InfoPics.Add(dbProfile.Result.ProfilePicture);
                foreach (var pic in dbProfile.Result.InfoPics)
                {
                    Db.UserMediaCollection.UpdateOne(x=>x.FullUrl==pic, new UpdateDefinitionBuilder<UserMedia>().Set(x=>x.ExpiresOn, DateTime.UtcNow.AddDays(1)));
                }

                profile.InfoPics.Add(profile.ProfilePicture);
                foreach (var pic in profile.InfoPics)
                {
                    Db.UserMediaCollection.UpdateOne(x => x.FullUrl == pic, new UpdateDefinitionBuilder<UserMedia>().Set(x => x.ExpiresOn, DateTime.MaxValue));
                }
            });

            using var s = Db.Client.StartSession();
            s.StartTransaction();

            Db.ProfilesCollection.UpdateOne(s, x => x.Id == profile.Id, new UpdateDefinitionBuilder<Profile>()
                .Set(x=>x.Email,profile.Email)
                .Set(x=>x.ProfilePicture, profile.ProfilePicture)
                .Set(x=>x.InfoPics, profile.InfoPics)
                .Set(x=>x.TextBio, profile.TextBio)
                .Set(x=>x.Skills, profile.Skills));
            Db.UsersCollection.UpdateOne(s, x => x.Id == user.Id, new UpdateDefinitionBuilder<User>()
                .Set(x=>x.Email, profile.Email));

            s.CommitTransaction();
            
            return Ok();
        }

        [HttpPost("createprofile")]
        public async Task<ActionResult<string>> CreateProfile(
            [FromForm] string token,
            [FromForm] string jprofile)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var user = await jwt.GetUserAsync();

            try
            {
                using var s = await Db.Client.StartSessionAsync();
                var profile = DeserializeObject<Profile>(jprofile);
                
                // verify data
                profile.Id = null;
                profile.Username = jwt.Username;
                profile.UserId = jwt.UserId;
                profile.BusinessType = user.BusinessType;

                if (profile.BusinessType != BusinessType.individual && string.IsNullOrWhiteSpace(profile.OrgName))
                    return BadRequest("Enter an company/organisation name");
                if (string.IsNullOrWhiteSpace(profile.FirstName) || string.IsNullOrWhiteSpace(profile.LastName))
                    return BadRequest("Enter a valid name");
                if (profile.DoB.AddYears(18) > DateTime.Today) 
                    return BadRequest("You need to be 18 years or older");
                if (profile.Skills.Count > 8) 
                    return BadRequest("You cannot add more than 8 skills, upgrade your account to have more");
                if (Db.ProfilesCollection.CountDocuments(x => x.UserId == jwt.UserId) > 0)
                    return BadRequest("Profile already exists");

                s.StartTransaction();

                Db.NotificationsCollections.InsertMany(s, new Notification[] 
                {
                    new Notification
                    {
                        Title = "Welcome to YupJobs!",
                        UserId = user.Id,
                        Clicked = false,
                        Seen = false,
                        Href = "/",
                        Description = $"Hey {profile.FirstName}! We hope you find what you need"
                    },
                    new Notification
                    {
                        Title = "For employers",
                        UserId = user.Id,
                        Clicked = false,
                        Seen = false,
                        Href = "/jobs/new",
                        Description = $"Post a new job offer"
                    },
                    new Notification
                    {
                        Title = "For freelancers",
                        UserId = user.Id,
                        Clicked = false,
                        Seen = false,
                        Href = "/profile/edit",
                        Description = $"We recommend completing your portfolio so that employers have an easier time to find you"
                    }
                });
                Db.UserMediaCollection.UpdateOne(s, x => x.FullUrl == profile.ProfilePicture, new UpdateDefinitionBuilder<UserMedia>().Set(x => x.ExpiresOn, DateTime.MaxValue));
                var method = new UserPaymentMethod()
                {
                    UserId = user.Id,
                    Default = true,
                    Type = MethodType.StripeInvoice,
                };
                Db.PaymentMethodsCollection.InsertOne(s, method);
                
                Db.ProfilesCollection.InsertOne(s, profile);
                Db.ProfileRatings.InsertOne(s, new ProfileRating
                {
                    ProfileId = profile.Id,
                    UserId = user.Id,
                });

                var customer = StripeController.CreateCustomer(user, profile, s);
                if (customer is null)
                {
                    _ = s.AbortTransactionAsync();
                    return BadRequest("There was an error, please try again");
                }

                s.CommitTransaction();

                _ = StripeController.CreateSofortMethod(user);
                _ = StripeController.CreateP24Method(user);
                

                return profile.ToJson();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                //_ = s.AbortTransactionAsync();
                return BadRequest(e);
            }
        }

        [HttpPost("getprofile/{profileId}")]
        public ActionResult<string> GetProfile(string profileId)
        {
            var profile = Db.ProfilesCollection.First(x => x.Id == profileId);
            if (profile is null) return NotFound();

            return profile.ToJson();
        }

        [HttpPost("profile/user/{id}")]
        public ActionResult<string> GetProfileByUser(string id)
        {
            var profile = Db.ProfilesCollection.First(x => x.UserId == id);
            if (profile is null) return NotFound();

            return profile.ToJson();
        }

        [HttpPost("getratings/{id}")]
        public ActionResult<string> GetRatings(string id)
        {
            var rating = Db.ProfileRatings.First(x => x.ProfileId == id);
            if (rating is null) return NotFound();

            return rating.ToJson();
        }

        [HttpPost("checkprofile")]
        public ActionResult<string> CheckProfile([FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            if (Db.ProfilesCollection.CountDocuments(x => x.UserId == jwt.UserId) > 0) return Ok();
            return NotFound();
        }

        [HttpPost("updateprofile")]
        public ActionResult<string> UpdateProfile(
            [FromForm] string token,
            [FromForm] string jprofile)
        {
            //TODO
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var profile = DeserializeObject<Profile>(jprofile);

            var id = profile.Id;
            profile.Id = null;

            Db.ProfilesCollection.UpdateOne(x => x.Id == id, new JsonUpdateDefinition<Profile>(SerializeObject(profile)));

            //if (string.IsNullOrWhiteSpace(profile.Username)
            //    || profile.Username.Length < 6
            //    && profile.)

            return Ok();
        } 
    }
}
