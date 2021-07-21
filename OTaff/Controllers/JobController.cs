using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
//using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Lib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace OTaff.Controllers
{
    [Route("api/job")]
    public class JobController : ControllerBase
    {
        [HttpPost("mine")]
        public ActionResult<dynamic> GetMine(
            [FromForm] string token,
            [FromForm] string id = null)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            if (id != null)
            {
                var job = Db.OngoingJobsCollection.Find(x => x.Id == id && (x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId)).FirstOrDefault();
                if (job is null) return NotFound();
                return job;
            }
            else
            {
                var jobs = Db.OngoingJobsCollection.Find(x => (x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId)).ToList();
                return jobs.ToJson();
            }
        }

        [HttpPost("post")]
        public async Task<ActionResult<string>> Post(
            [FromForm] string token,
            [FromForm] string jpost)
        {
            var jwt = DeserializeObject<Jwt>(token);

            var user = await jwt.GetUserAsync();
            var sub = await Db.SubscriptionsCollection.FirstAsync(x => x.UserId == jwt.UserId);

            if (!jwt.Verify()) return Unauthorized();

            var post = jpost.ToObject<JobPost>();

            if (!post.VerifyContent(sub.Type))
                return BadRequest("Please verify that all the fields are entered correctly");

            post.UserId = user.Id;
            post.Username = user.Username;

            var limits = SubscriptionsMeta.Limits[sub.Type];

            if (Db.JobPostsCollection.CountDocuments(x => x.UserId == jwt.UserId) >= limits.JobPostLimit)
                return BadRequest("Too many posts");

            post.MaxApplies = limits.JobPostApplicationLimit;
            post.Tags = post.Tags.Take(limits.PostTagCount).ToList();

            Db.JobPostsCollection.InsertOne(post);

            return post.Id;
        }

        [HttpPost("hiresearch")]
        public ActionResult<string> HireSearch(
            [FromForm] string token,
            [FromForm] string employeeId,
            [FromForm] string jobName)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            using var s = Db.Client.StartSession();
            var emp = Db.UsersCollection.First(x => x.Id == employeeId);
            var epr = jwt.GetUser();

            s.StartTransaction();
            var conv = new Conversation()
            {
                Title = jobName,
                Created = DateTime.UtcNow,
                Members = new List<ConversationMember>
                {
                    new ConversationMember{ UserId = jwt.UserId, Username = jwt.Username },
                    new ConversationMember{ UserId = emp.Id, Username = emp.Username },
                },
            };
            Db.ConversationsCollection.InsertOne(s, conv);

            var job = new Job() 
            { 
                Active = true,
                ConversationId = conv.Id,
                //Currency = currency,
                Status = JobStatus.Started,
                EmployeeId = emp.Id,
                EmployerId = jwt.UserId,
                IsPaidOnPlatform = true,
                StartedOn = DateTime.UtcNow,
                JobTitle = jobName,
            };
            Db.OngoingJobsCollection.InsertOne(s, job);

            s.CommitTransaction();
            return job.Id;
        }

        [HttpPost("hire")]
        public async Task<ActionResult<string>> HireApplicant(
            [FromForm] string token,
            [FromForm] string appId,
            [FromForm] string postId = null)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            using var s = await Db.Client.StartSessionAsync();
            var app = await Db.JobAppliesCollection.Find(x => x.Id == appId).FirstOrDefaultAsync();
            var post = postId is null ? null : await Db.JobPostsCollection.Find(x => x.Id == postId).FirstOrDefaultAsync();

            if (app is null || app?.PostId != postId) return BadRequest("Application does not exist");
            if (post is null && postId != null || post.UserId != jwt.UserId) return BadRequest("Post does not exist");

            var conv = new Conversation()
            {
                Title = post.Title,
                Created = DateTime.UtcNow,
                Members = new List<ConversationMember>
                {
                    new ConversationMember{ UserId = jwt.UserId, Username = jwt.Username },
                    new ConversationMember{ UserId = app.UserId, Username = app.Username },
                },
            };

            s.StartTransaction();


            Db.ConversationsCollection.InsertOne(s, conv);
            Db.MessagesCollection.InsertOne(s, new ChatMessage {
                DateSent = DateTime.UtcNow,
                Content = "THE JOB HAS STARTED",
                ConvId = conv.Id,
            });

            var job = new Job()
            {
                ConversationId = conv.Id,
                EmployeeId = app.UserId,
                EmployerId = post.UserId,
                IsPaidOnPlatform = false,
                JobTitle = post.Title,
                OriginalPost = post,
                //Payment = payment,
                Currency = post.Currency,
            };
            Db.OngoingJobsCollection.InsertOne(s, job);

            Db.NotificationsCollections.InsertOne(s, new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"You have been hired by {jwt.Username} for job {job.JobTitle}",
                Title = "Congratulations! You have been hired",
                Href = $"/job/ongoing/{job.Id}",
                UserId = app.UserId
            });

            s.CommitTransaction();
            _ = Task.Run(() => {
                _ = MailController.SendMail(
                   $"You have been hired for {job.JobTitle}, congratulations! <a href='https://www.yupjobs.net/job/ongoing/{job.Id}'>GO TO JOB</a>",
                   "You have been hired",
                   Db.UsersCollection.First(x => x.Id == app.UserId).Email);
            });

            return job.Id;
        }

        public static short MinEurPay = 10;

        [HttpPost("cancelpayment")]
        public ActionResult<string> CancelPayment(
            [FromForm] string token,
            [FromForm] string paymentId,
            [FromForm] string jobId)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var job = Db.OngoingJobsCollection.First(x => x.Id == jobId && x.EmployerId == jwt.UserId);
            var payment = Db.JobPaymentsCollection.First(x => x.Id == paymentId && x.JobId == jobId && x.EmployerId == jwt.UserId);
            using var s = Db.Client.StartSession();
            s.StartTransaction();
            WalletCtl.CreateTransferFromPlatform(jwt.GetUser(), payment.Currency, payment.TransferAmount, 0, s).Wait();
            Db.JobPaymentsCollection.DeleteOne(s, x=>x.Id==payment.Id);
            Db.NotificationsCollections.InsertOne(s, new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"The payment of {payment.TransferAmount} {Enum.GetName(payment.Currency)} cancelled for {job.JobTitle}",
                UserId = jwt.UserId,
                Href = $"/job/ongoing/{job.Id}",
                Title = "Payment cancelled"
            });

            s.CommitTransaction();

            return Ok();
        }

        [HttpPost("addpayment")]
        public async Task<ActionResult<string>> AddPayment(
            [FromForm] string token,
            [FromForm] string methodId,
            [FromForm] string jobId,
            [FromForm] string jpayments,
            [FromForm] Currency currency)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var user = await jwt.GetUserAsync();
            var method = methodId is "WALLET" ? new UserPaymentMethod { Id = "WALLET", MethodId = "WALLET", Type = MethodType.YupWallet} : await Db.PaymentMethodsCollection.Find(x => x.Id == methodId).FirstOrDefaultAsync();
            var job = Db.OngoingJobsCollection.Find(x => x.Id == jobId && x.EmployerId == jwt.UserId).FirstOrDefault();

            if (job is null) return NotFound("Job does not exist");
            if (method is null) return NotFound("Please select a valid payment method");
            var payments = DeserializeObject<Dictionary<string, decimal>>(jpayments);

            decimal total = payments.Values.Sum();

            decimal minCurrencyPay = CurrencyConversion.GetConvertedRate(MinEurPay, currency);
            if (total <= minCurrencyPay) return BadRequest($"The total of all payments must be of more than {minCurrencyPay} {Ez.GetName(currency)}");

            var fees = FeesCtl.WalletRechargeFee(total, currency);

            var action = new JobPaymentActionData
            {
                ActionType = BillAction.AddJobPayment,
                EmployeeId = job.EmployeeId,
                EmployerId = job.EmployerId,
                Executed = false,
                Currency = currency,
                Issued = DateTime.UtcNow,
                JobId = job.Id,
                Payments = payments,
                UserId = user.Id,
                Description = "Job payment recharge",
            };

            var ret = ChargeCtl.ChargeMethod(
                user,
                currency,
                method,
                method.Type == MethodType.YupWallet ? new[] { 0m, payments.Values.Sum(), 0m} : fees,
                //method.Type is MethodType.YupWallet ? payments.Values.Sum() : fees[1],
                $"Payment for job {job.Id} from {jwt.Username}",
                action: action,
                failKeep: true);

            Db.NotificationsCollections.InsertOne(new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"Total payments of {payments.Values.Sum()} {Enum.GetName(currency)} added for {job.JobTitle}",
                UserId = jwt.UserId,
                Href = $"/job/ongoing/{job.Id}",
                Title = "Escrow payment added"
            });

            return ret.ToJson();
        }

        [HttpPost("update")]
        public ActionResult<string> UpdateJob(
            [FromForm] string token,
            [FromForm] string id,
            [FromForm] JobStatus status)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

          /*  if (!new[] { JobStatus.Completed, JobStatus.Cancelled, JobStatus.OnHold, JobStatus.Disputed }.Contains(status))
                return BadRequest("Invalid status");*/

            using var jobT = Db.OngoingJobsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            var user = jwt.GetUser();
            var job = jobT.Result;

            switch (status)
            {
                case JobStatus.Completed:
                    //payout all job payments
                    CompleteJob(user, job);
                    break;
                //break;
                case JobStatus.OnHold:
                    HoldJob(user, job);
                    break;
                //break;
                case JobStatus.Disputed:
                    DisputeJob(user, job);
                    break;
                //break;
                default:
                    return BadRequest("Invalid status");
            }

            return Ok();
        }

        [HttpPost("payments")]
        public ActionResult<string> MyPayments(
            [FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            return Db.JobPaymentsCollection.All(x => x.EmployeeId == jwt.UserId || x.EmployerId == jwt.UserId).ToJson();
        }

        [HttpPost("release")]
        public async Task<ActionResult<string>> ReleasePayment(
            [FromForm] string token,
            [FromForm] string paymentId,
            [FromForm] string jobId)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            using var s = await Db.Client.StartSessionAsync();
            var job = await Db.OngoingJobsCollection.Find(x => x.Id == jobId && x.EmployerId == jwt.UserId).FirstOrDefaultAsync();
            var payment = await Db.JobPaymentsCollection.Find(x => x.Id == paymentId && x.JobId == jobId && x.EmployerId == jwt.UserId && !x.Released && x.Paid).FirstOrDefaultAsync();

            var user = await Db.UsersCollection.FirstAsync(x => x.Id == job.EmployeeId);
            if (job is null) return BadRequest("Job not found");
            if (payment is null) return BadRequest("Payment not found");
            if (user is null) return BadRequest("User not found");
            s.StartTransaction();

            var transaction = await WalletCtl.CreateTransferFromPlatform(user, payment.Currency, payment.TransferAmount, fee: 0.043f, s: s);
            var upd = new UpdateDefinitionBuilder<JobPayment>().Set(x => x.Released, true).Set(x => x.DateReleased, DateTime.UtcNow);
            Db.JobPaymentsCollection.UpdateOne(s, x=>x.Id==payment.Id, upd);
            payment.Released = true;
            payment.DateReleased = DateTime.UtcNow;

            if (!transaction)
            {
                s.AbortTransactionAsync();
                return BadRequest("Transaction could not be created");
            }

            s.CommitTransaction();
            return payment.ToJson();
        }

        [HttpPost("directpay")]
        public async Task<ActionResult<string>> DirectPay(
            [FromForm] string token, 
            [FromForm] string jobId,
            [FromForm] string methodId, 
            [FromForm] decimal amount,
            [FromForm] Currency currency)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var user = await jwt.GetUserAsync();
            var job = await Db.OngoingJobsCollection.FirstAsync(x => x.Id == jobId);
            var method = methodId != "WALLET" ? await Db.PaymentMethodsCollection.FirstAsync(x => x.Id == methodId) : new UserPaymentMethod { 
                MethodId = "WALLET", Type = MethodType.YupWallet, Id = "WALLET"
            };
            var total = FeesCtl.WalletRechargeFee(amount, currency);

            //var wallet = Db.UserWalletsCollection.First(x => x.UserId == job.EmployeeId && x.Currency == currency);
            var wallet = await WalletCtl.GetUserWallet(job.EmployeeId, currency);
            //if (wallet is null)
            //{
            //    wallet = new UserWallet()
            //    {
            //        UserId = job.EmployeeId,
            //        Currency = currency,
            //        Funds = 0,
            //    };
            //    Db.UserWalletsCollection.InsertOne(wallet);
            //}

            var ret = ChargeCtl.ChargeMethod(
                user,
                currency,
                method,
                method.Type is MethodType.YupWallet ? new[] {0m,total[2],0m} : total,
                $"Payment for job {job.Id} from {jwt.Username}",
                //action: new WalletRechargeActionData()
                //{
                //    Currency = currency,
                //    Amount = amount,
                //    ActionType = BillAction.RechargeWallet,
                //    WalletId = wallet.Id,
                //    Executed = false,
                //    Issued = DateTime.UtcNow,
                //    UserId = jwt.UserId,
                //}
                action: new JobPaymentActionData
                {
                    ActionType = BillAction.AddJobPayment,
                    AutoRelease = true,
                    Payments = new Dictionary<string, decimal>
                    {
                        {$"Direct payment of {amount} {currency.GetName()}", amount},
                    },
                    Currency = currency,
                    EmployeeId = job.EmployeeId,
                    EmployerId = job.EmployerId,
                    JobId = job.Id,
                    Executed = false,
                    Issued = DateTime.UtcNow,
                    UserId = user.Id,
                }
                );

            Db.NotificationsCollections.InsertOne(new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"Direct payment of {amount} {Enum.GetName(currency)} added for {job.JobTitle}",
                UserId = jwt.UserId,
                Href = $"/wallets",
                Title = "Payment added"
            });

            return ret.ToJson();
        }

        /// <summary>
        /// Will auto payout
        /// </summary>
        /// <param name="user"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        public void CompleteJob(User user, Job job)
        {
            var upd = new UpdateDefinitionBuilder<Job>()
                .Set(x => x.Status, JobStatus.Completed);
            Db.OngoingJobsCollection.UpdateOne(x => x.Id == job.Id, upd);
            Db.NotificationsCollections.InsertOne(new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"{job.JobTitle} set as completed by {user.Username}",
                UserId = job.EmployeeId == user.Id ? job.EmployerId : job.EmployeeId,
                Href = $"/job/ongoing/{job.Id}",
                Title = "Job completed"
            });
        }

        public void HoldJob(User user, Job job)
        {
            var upd = new UpdateDefinitionBuilder<Job>()
                .Set(x => x.Status, JobStatus.OnHold);
            Db.OngoingJobsCollection.UpdateOne(x => x.Id == job.Id && (x.EmployeeId == user.Id || x.EmployeeId == user.Id), upd);
            Db.NotificationsCollections.InsertOne(new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"{job.JobTitle} set as completed by {user.Username}",
                UserId = job.EmployeeId == user.Id ? job.EmployerId : job.EmployeeId,
                Href = $"/job/ongoing/{job.Id}",
                Title = "Job completed"
            });
        }
        public void DisputeJob(User user, Job job)
        {
            using var s = Db.Client.StartSession();

            var emp = Db.UsersCollection.First(x => x.Id == job.EmployeeId);


            var conv = new Conversation
            {
                Created = DateTime.UtcNow,
                Members = new List<ConversationMember> { 
                    new ConversationMember { UserId = job.EmployeeId, Username = emp.Username},
                    new ConversationMember { UserId = job.EmployerId, Username = job.OriginalPost.Username}
                }
            };

            s.StartTransaction();
            Db.ConversationsCollection.InsertOne(s, conv);
            Db.MessagesCollection.InsertOne(s, new ChatMessage
            {
                ConvId = conv.Id,
                DateSent = DateTime.UtcNow,
                UserId = "PLATFORM",
                Content = "A dispute has been started, staff will be assigned soon",
            });

            var upd = new UpdateDefinitionBuilder<Job>()
                .Set(x => x.Status, JobStatus.Disputed)
                .Set(x => x.ConversationId, conv.Id);
            Db.NotificationsCollections.InsertOne(s, new Notification
            {
                Date = DateTime.UtcNow,
                Description = $"{job.JobTitle} set as completed by {user.Username}",
                UserId = job.EmployeeId == user.Id ? job.EmployerId : job.EmployeeId,
                Href = $"/job/ongoing/{job.Id}",
                Title = "Job completed"
            });

            Db.Disputes.InsertOne(s, new SharedLib.Models.Dispute
            { 
                ConversationId = conv.Id,
                DateStarted = DateTime.UtcNow,
                UserIds = new List<(string, string)>
                {
                    (job.EmployeeId, emp.Username),
                    (job.EmployerId, job.OriginalPost.Username)
                },
                JobId = job.Id,
                StaffAssigned = false
            });
            Db.OngoingJobsCollection.UpdateOne(s, x => x.Id == job.Id, upd);

            s.CommitTransaction();
        }

        //[HttpPost("new")]
    }
}
