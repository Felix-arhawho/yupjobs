using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public class Conversation
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }

        public List<ConversationMember> Members { get; set; } = new List<ConversationMember>();
        public List<ConversationMember> Moderators { get; set; } = new List<ConversationMember>();

        public DateTime Created { get; set; }
    }

    public class ConversationMember
    {
        public string Username { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string UserId { get; set; }
    }

    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ConvId { get; set; }

        public string Content { get; set; }
        public DateTime DateSent { get; set; }
        public bool Hidden { get; set; } = false;
        public bool IsStaff { get; set; }
        public bool Seen { get; set; }
    }


}
