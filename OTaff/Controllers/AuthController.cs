using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using SharedLib.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using static Newtonsoft.Json.JsonConvert;
using OTaff.Lib.Money;
using SharedLib.Lib;
using ServerLib;

namespace OTaff.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public AuthController()
        {
            _ = Task.Run(CleanupAttemps);
        }

        [HttpPost("reauth")]
        public ActionResult<string> ReAuth([FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var tok = HashingManager.RandomString(128);
            var profile = Db.ProfilesCollection.First(x => x.UserId == jwt.UserId);
            Db.JwtsCollection.DeleteOneAsync(x => x.Id == jwt.Id);
            jwt = new Jwt()
            {
                Token = HashingManager.HashToString(tok),
                Email = jwt.Email,
                Issued = jwt.Issued,
                ProfileId = profile.Id,
                UserId = jwt.UserId,
                Username = jwt.Username
            };

            Db.JwtsCollection.InsertOne(jwt);
            jwt.Token = tok;
            return jwt.ToJson();
        }

        [HttpPost("verify")]
        public ActionResult<string> VerifyJwt([FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (jwt.Verify()) return Ok();
            return BadRequest();
        }

        private static string ReqPwdChars = "&@!+=#";
        private static string ReqPwdNums = "0123456789";
        private static string Caps = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        [HttpPost("checkusername")]
        public ActionResult<string> CheckUsername([FromForm] string username, [FromForm] string apiKey = "apikey")
        {
            if (username.Length > 15) return "USED";
            if (Db.UsersCollection.CountDocuments(x => x.Username == username) > 0) return "USED";
            return "FREE";
        }

        [HttpPost("checkemail")]
        public ActionResult<string> CheckEmail([FromForm] string email)
        {
            if (Db.UsersCollection.CountDocuments(x => x.Email == email) > 0) return "USED";
            return "FREE";
        }

        [HttpPost("resetpwd")]
        public ActionResult<dynamic> ResetPassword([FromForm] string login, [FromForm] string password)
        {
            //TODO DDOS

            var user = Db.UsersCollection.Find(x => x.Username == login || x.Email == login).FirstOrDefault();
            if (user is null) return BadRequest("User does not exist");

            if (Db.OtpCollection.CountDocuments(x => x.UserId == user.Id && x.Issued > DateTime.UtcNow - TimeSpan.FromMinutes(30)) > 0)
                return BadRequest("You need to wait 30 minutes before you can ask for a reset again");

            var otp = new Otp()
            {
                Code = HashingManager.RandomString(6),
                Type = OtpType.PwdReset,
                UserId = user.Id,
                Issued = DateTime.UtcNow,
                Meta = new Dictionary<string, string> {
                    { "password", HashingManager.HashToString(password) },
                    { "id", user.Id },
                }
            };
            Db.NotificationsCollections.InsertOne(new Notification
            {
                Date = DateTime.UtcNow,
                UserId = user.Id,
                Title = "Password changed",
                Description = $"Password changed on {DateTime.Today.ToShortDateString()}",
            });

            Db.OtpCollection.InsertOne(otp);

            string content =
@$"
<div>
<h3>Click this link to change your password:</h3>

<a href='{Ez.ApiUrl}auth/resetpwdact/{otp.Id}/{otp.Code}'>CLICK TO RESET PASSWORD</a>

</div>
";
            /*{Ez.ApiUrl} auth/resetpwdact/{otp.Id}/{otp.Code}*/
            _ = MailController.SendMail(content, "Password reset for Otaff", user.Email);
            return "Check your email for confirmation";
        }

        [HttpGet("resetpwdact/{id}/{code}")]
        public ActionResult<string> ResetPwdAct(string id, string code)
        {
            //DDOS

            var otp = Db.OtpCollection.Find(x => x.Id == id && x.Code == code && x.Type == OtpType.PwdReset).FirstOrDefault();
            if (otp is null) return Redirect("https://yupjobs.net/error");

            Db.UsersCollection.UpdateOne(x => x.Id == otp.Meta["id"], new UpdateDefinitionBuilder<User>().Set("HashedPassword", otp.Meta["password"]));
            Db.OtpCollection.UpdateOneAsync(x => x.Id == id, new UpdateDefinitionBuilder<Otp>().Set(x => x.Code, HashingManager.RandomString(8)));

            return Redirect("https://yupjobs.net/success");
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(
            [FromForm] string username,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] Currency currency,
            [FromForm] CountryCode country,
            [FromForm] BusinessType businessType,
            [FromForm] string phone = null,
            [FromForm] string backupEmail = null)
        {
            using var s = await Db.Client.StartSessionAsync();

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required");
            if (string.IsNullOrEmpty(email)
                || !email.Contains('@')
                || !email.Contains('.'))
                return BadRequest("This email is not valid");
            if (password.Length < 6 || !password.Any(x => ReqPwdNums.Contains(x))
                /*|| !password.Any(x => ReqPwdChars.Contains(x)) ||  || password.Any(x=>Caps.Contains(x))*/)
                return BadRequest("Password is not valid, please ensure that it is longer than 6 characters you have at least one of '&@!+=#', a capital letter and a number");
            if (Db.UsersCollection.CountDocuments(x => x.Email == email || x.Username == username) > 0) return BadRequest("Username or email is already used");

            s.StartTransaction();

            var user = new User {
                DateCreated = DateTime.UtcNow,
                Email = email,
                BackupEmail = backupEmail,
                Phone = phone,
                Username = new string(username.Take(128).ToArray()),
                HashedPassword = HashingManager.HashToString(password),
                Verified = false,
                Country = country,
                DefaultCurrency = currency,
                BusinessType = businessType,
            };
            Db.UsersCollection.InsertOne(s, user);

            var subscription = new UserSubscription() {
                //IsValid = false,
                //StripeSubscriptionId = "free",
                UserId = user.Id,
                Type = SubscriptionType.Free,
                ValidUntil = DateTime.MinValue,

            };
            Db.SubscriptionsCollection.InsertOne(s, subscription);

            var otp = new Otp()
            {
                UserId = user.Id,
                Code = HashingManager.RandomString(6),
                Type = OtpType.Register,
                Issued = DateTime.UtcNow
            };
            Db.OtpCollection.InsertOne(s, otp);

            //var stripeId = StripeController.CreateCustomer(user);

            Db.UserWalletsCollection.InsertOne(s, new UserWallet {
                Created = DateTime.UtcNow,
                Currency = user.DefaultCurrency,
                Funds = 0,
                Hidden = false,
                UserId = user.Id,
                Purpose = WalletPurpose.General,
            });

            s.CommitTransaction();

            string content =
@$"
<div>
<h3>Click this link to verify =></h3>

<a href='{Ez.ApiUrl}auth/verifyotp/{otp.Id}/{otp.Code}/'>{Ez.ApiUrl}auth/verifyotp/{otp.Id}/{otp.Code}/<a>

<h3>Your otp for registration is: <b>{otp.Code}</b></h3>
</div>
";

            _ = MailController.SendMail(content, "Confirm you account for YupJobs", user.Email);

            user.HashedPassword = null;
            return user.ToJson();
        }

        [HttpGet("verifyotp/{id}/{code}")]
        public ActionResult<string> VerifyOtp(string id, string code)
        {
            var otp = Db.OtpCollection.Find(x => x.Id == id && code == x.Code).FirstOrDefault();
            if (otp is null) return Redirect("https://www.yupjobs.net/");

            using var s = Db.Client.StartSession();
            s.StartTransaction();
            switch (otp.Type)
            {
                case OtpType.Register:
                    var upd = new UpdateDefinitionBuilder<User>().Set("Verified", true);
                    Db.UsersCollection.UpdateOne(s, x => x.Id == otp.UserId, upd);
                    Db.OtpCollection.DeleteOne(s, x => x.Id == otp.Id);
                    s.CommitTransaction();
                    return Redirect("https://www.yupjobs.net/verifysuccess");

                default:
                    s.AbortTransaction();
                    return Redirect("https://www.yupjobs.net/notfound");
            }
        }

        [HttpPost("remailconf")]
        public void ResendEmailConfirmation([FromForm] string login)
        {
            var user = Db.UsersCollection.First(x => x.Username == login || x.Email == login);
            if (user is null) return;
            if (Db.OtpCollection.CountDocuments(x => x.UserId == user.Id && x.Type == OtpType.Register && x.Issued > DateTime.UtcNow - TimeSpan.FromMinutes(30)) > 3)
                return;

            var otp = new Otp()
            {
                Code = HashingManager.RandomString(4),
                Issued = DateTime.UtcNow,
                UserId = user.Id,
                Type = OtpType.Register
            };
            Db.OtpCollection.InsertOne(otp);

            string content =
@$"
<div>
<h3>Click this link to verify =></h3><br/>
<a href='{Ez.ApiUrl}auth/verifyotp/{otp.Id}/{otp.Code}/'>{Ez.ApiUrl}auth/verifyotp/{otp.Id}/{otp.Code}/<a>
</div>
";
            _ = MailController.SendMail(content, "Confirm you account for Otaff", user.Email);
        }

        [HttpPost("login")]
        public ActionResult<string> Login(
            [FromForm] string login,
            [FromForm] string password)
        {
            lock (AttemptsLock) LoginAttempts.Add((HttpContext.Request.Host, DateTime.UtcNow, login));

            if (LoginAttempts.Count(x => x.Item3 == login && x.Item2 > DateTime.UtcNow - TimeSpan.FromMinutes(5)) > 5)
                return BadRequest("Too many login attemps");

            var user = Db.UsersCollection.Find(x => x.Username == login || x.Email == login).FirstOrDefault();
            var profile = Db.ProfilesCollection.FirstAsync(x => x.UserId == user.Id);
            if (user is null) return BadRequest("Incorrect username/password");

            if (!HashingManager.Verify(password, user.HashedPassword))
                return BadRequest("Incorrect username/password");
            if (!user.Verified)
                return StatusCode(499, "Please check your email for an account confirmation link and try again");

            if (user.TwoFactorAuth)
            {
                var code = HashingManager.RandomString(4);
                var otp = new Otp()
                {
                    Code = code,
                    Issued = DateTime.UtcNow,
                    Type = OtpType.Login,
                    UserId = user.Id
                };
                Db.OtpCollection.InsertOne(otp);

                MailController.SendMail($"Your otp is <b>{otp.Code}</b>", "Login to YupJobs", user.Email);

                return $"{otp.Id}";
            }

            var token = HashingManager.RandomString(128);

            var jwt = new Jwt()
            {
                Email = user.Email,
                Issued = DateTime.UtcNow,
                Token = HashingManager.HashToString(token),
                UserId = user.Id,
                Username = user.Username,
                ProfileId = profile.Result?.Id
            };

            Db.JwtsCollection.InsertOne(jwt);
            jwt.Token = token;

            return jwt.ToJson();
        }

        [HttpPost("2falogin")]
        public ActionResult<string> TwoFA([FromForm] string otp, [FromForm] string id)
        {
            var dbc = Db.OtpCollection.Find(x => x.Id == id && x.Code == otp).FirstOrDefault();
            if (dbc is null) return BadRequest();
            var token = HashingManager.RandomString(128);

            var profile = Db.ProfilesCollection.FirstAsync(x => x.UserId == dbc.UserId);
            var user = Db.UsersCollection.Find(x => x.Id == dbc.UserId).FirstOrDefault();

            var jwt = new Jwt()
            {
                Email = user.Email,
                Issued = DateTime.UtcNow,
                Token = HashingManager.HashToString(token),
                UserId = user.Id,
                Username = user.Username,
                ProfileId = profile.Result?.Id
            };

            Db.JwtsCollection.InsertOne(jwt);
            Db.OtpCollection.DeleteManyAsync(x => x.UserId==user.Id);
            
            jwt.Token = token;

            return jwt.ToJson();
        }

        [HttpPost("resend2facode")]
        public ActionResult<string> Resend2FACode([FromForm] string id)
        {
            var otp = Db.OtpCollection.First(x => x.Id == id);
            var user = Db.UsersCollection.First(x => x.Id == otp.UserId);

            var code = HashingManager.RandomString(4);
            otp = new Otp()
            {
                Code = code,
                Issued = DateTime.UtcNow,
                Type = OtpType.Login,
                UserId = user.Id
            };
            Db.OtpCollection.InsertOne(otp);

            MailController.SendMail($"Your otp is <b>{otp.Code}</b>", "Login to YupJobs", user.Email);

            return $"{otp.Id}";

        }


        private static object AttemptsLock = new object();
        static bool Started = false;
        private static async Task CleanupAttemps()
        {
            if (Started) return;
            Started = true;
            while (true)
            {
                Task.Delay(1000 * 60 * 3).Wait();

                lock (AttemptsLock)
                    LoginAttempts.RemoveAll(x => x.Item2 < DateTime.UtcNow-TimeSpan.FromMinutes(5));   
            }
        }

        private static List<(HostString, DateTime, string)> LoginAttempts 
            = new List<(HostString, DateTime, string)>();
    }
}
