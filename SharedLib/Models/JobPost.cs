using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SharedLib.Models
{
    public class JobPost
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }

        public bool Active { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime PostAutoRemoveDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public short WorkDuration { get; set; }
        //public TimeSpan Duration { get; set; }

        public decimal[] ProposedSalary { get; set; } = new decimal[] { 
            100,
            300
        };
        public Currency Currency { get; set; }
        //public List<KeyValuePair<string, decimal>> Payments { get; set; } = new List<KeyValuePair<string, decimal>>();

        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Images { get; set; } = new List<string>();

        public string Username { get; set; }
        public List<JobCategory> Categories { get; set; } = new List<JobCategory>();
        public short MaxApplies { get; set; }

        public long Views { get; set; } = 0;

        public bool Promoted { get; set; } = false;
    }

    public class JobSearchPost
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string UserId { get; set; }

        public DateTime PostDate { get; set; }
        public DateTime PostAutoRemoveDate { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public List<JobCategory> Categories { get; set; } = new List<JobCategory>();
        public decimal[] RequestedSalary { get; set; } = new decimal[1];
        public Currency Currency { get; set; }
        public bool Active { get; set; }
        public string ProfileId { get; set; }
        public string Username { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public short MaxApplies { get; set; }
        public string Country { get; set; }
        public List<(string, short)> Experience { get; set; } = new List<(string, short)>();

        public long Views { get; set; } = 0;
        public bool Promoted { get; set; }
    }

    public enum JobCategory
    {
        Any = 100,
        Misc = 0,
        
        //software
        Development = 1,
        Fullstack = 11,
        Frontend = 12,
        Backend = 13,
        Data = 2,
        Graphics = 3,
        Games = 4,
        Tutoring = 5,
        Images = 103,
        Software = 1290,
        Design = 203,
        Translation = 2932,
        Modeling = 9293,

    }

    public enum JobType
    {
        Freelance = 0,
        Position = 1,
        Tutoring = 2
    }

    public class Report
    {
        [BsonId][BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string Reason { get; set; }
        public string TargetId { get; set; }
        public ReportType Type { get; set; }
        public DateTime Date { get; set; }

        public string StaffId { get; set; }
    }

    public enum ReportType
    {
        JobPost = 1202,
        JobSearchPost = 211,
        User = 102,
        Conversation = 10,
        Application = 2092,
        Proposal = 1030
    }

    public class JobPostFilter
    {
        public List<JobCategory> Categories = new List<JobCategory>() { JobCategory.Any };
        public string Search = string.Empty;
        public List<string> Tags = new List<string>();
        /// <summary>
        /// In days
        /// </summary>
        public int MaxDuration = 30;
        public DateTime MaxDate = DateTime.UtcNow - TimeSpan.FromDays(60);
        public decimal MinimumSalary = 50;
        public short MaxCount = 40;
    }
}
