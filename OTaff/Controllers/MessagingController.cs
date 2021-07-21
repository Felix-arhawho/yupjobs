using Microsoft.AspNetCore.Mvc;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json;
using static Newtonsoft.Json.JsonConvert;
using ServerLib;

namespace OTaff.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        [HttpPost("newconv")]
        public ActionResult<string> NewConversation(
            [FromForm] string token,
            //target userId
            [FromForm] string target,
            [FromForm] string jmessage)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var message = DeserializeObject<ChatMessage>(jmessage);

            //if (Db.UsersCollection.CountDocuments(x => x.Id == target) != 1)
            var targetUser = Db.UsersCollection.First(x => x.Id == target);
            if (targetUser is null) return BadRequest("Target user does not exist");

            if (string.IsNullOrWhiteSpace(message.Content)) return BadRequest("Message is blank");

            var conv = new Conversation()
            {
                Created = DateTime.UtcNow,
                Members = new List<ConversationMember> {
                    new ConversationMember { UserId = jwt.UserId, Username = jwt.Username}, 
                    new ConversationMember { UserId = targetUser.Id, Username = targetUser.Username}
                },
                Title = $"{jwt.Username} - {targetUser.Username}",
            };
            Db.ConversationsCollection.InsertOne(conv);

            message.Id = null;
            message.ConvId = conv.Id;
            message.DateSent = DateTime.UtcNow;
            _ = Db.MessagesCollection.InsertOneAsync(message);
            _ = Db.NotificationsCollections.InsertManyAsync(new List<Notification> 
            {
                new Notification
                {
                    UserId = jwt.UserId,
                    Title = "New conversation",
                    Description = "Click to see",
                    Href = "/chat/"+conv.Id,
                    Date = DateTime.UtcNow,
                },
                new Notification
                {
                    UserId = targetUser.Id,
                    Title = "New conversation",
                    Description = "Click to see",
                    Href = "/chat/"+conv.Id,
                    Date = DateTime.UtcNow,
                }
            });

            return conv.Id;
        }

        [HttpPost("getmessages")]
        public ActionResult<string> GetMessages(
            [FromForm] string token,
            [FromForm] string convId = null,
            [FromForm] DateTime? since = null)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            if (since is null) since = DateTime.UtcNow - TimeSpan.FromDays(60);

            if (convId is null)
            {
                var convs = Db.ConversationsCollection.All(x => x.Members.Contains(new ConversationMember { UserId = jwt.UserId, Username = jwt.Username })).Select(x=>x.Id);
                return Db.MessagesCollection.All(x => convs.Contains(x.ConvId) && x.DateSent > DateTime.UtcNow - TimeSpan.FromDays(230)).ToJson();
            }
            else
            {
                var conv = Db.ConversationsCollection
                    .Find(x => x.Id == convId && x.Members.Contains(new ConversationMember { UserId = jwt.UserId, Username = jwt.Username})).FirstOrDefault();
                if (conv is null) return BadRequest("Conversation does not exist");
                var messages = Db.MessagesCollection
                    .Find(x => x.ConvId == convId && x.DateSent >= since).ToList();
                return messages.ToJson();
            }
        }

        [HttpPost("conversations")]
        public ActionResult<string> GetConverstations([FromForm] string token)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            var convs = Db.ConversationsCollection.All(x => x.Members.Contains(new ConversationMember { UserId = jwt.UserId, Username = jwt.Username }));
            return convs.ToJson();
        }

        [HttpPost("hidemessage")]
        public ActionResult<string> HideMessage(
            [FromForm] string token,
            [FromForm] string id)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();
            var upd = Db.MessagesCollection.UpdateOne(x => x.Id == id && x.UserId == jwt.UserId, new UpdateDefinitionBuilder<ChatMessage>().Set("Hidden", true));
            return StatusCode(200);
        }

        [HttpPost("sendmessage")]
        public ActionResult<string> SendMessage(
            [FromForm] string token,
            [FromForm] string jmessage)
        {
            var jwt = DeserializeObject<Jwt>(token);
            if (!jwt.Verify()) return Unauthorized();

            var message = jmessage.ToObject<ChatMessage>();

            if (message.ConvId is null) return BadRequest("Conversation ID was not provided");

            if (string.IsNullOrWhiteSpace(message.Content)) return BadRequest("Message content is blank");

            var conv = Db.ConversationsCollection.First(x => x.Id == message.ConvId);
            if (conv is null /*|| !conv.Members.Contains(new ConversationMember { UserId = jwt.UserId, Username = jwt.Username})*/) return BadRequest("Conversation does not exist");
            
            message.Id = null;
            message.DateSent = DateTime.UtcNow;
            message.UserId = jwt.UserId;

            Db.MessagesCollection.InsertOne(message);

            return StatusCode(200);
        }
    }
}

