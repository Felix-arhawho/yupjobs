using MongoDB.Driver;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Extensions
{
    public static class PostFinder
    {
        public static List<JobPost> FindHirePosts(
            Profile profile,
            DateTime maxDate,
            List<JobCategory> categories, 
            short count,
            string search = null,
            List<string> tags = null)
        {
            //if (maxDate is null) maxDate = DateTime.Today - TimeSpan.FromDays(60);

            var results = Db.JobPostsCollection.FindSync(x =>
                x.Categories.Any(g => categories.Contains(g))
                && search == null ? true : x.Title.Contains(search)
                && x.Active
                && x.PostDate >= maxDate
                && tags == null ? true : x.Tags.Any(g => tags.Contains(g)),
                new FindOptions<JobPost, JobPost>() {
                    Sort = new SortDefinitionBuilder<JobPost>().Descending(x => x.PostDate),
                    Limit = count,
                    BatchSize = 100
                }).ToList();

            return new List<JobPost>();
        }

        public static List<JobSearchPost> FindJobSeekers()
        {


            return new List<JobSearchPost>();
        }

        //public static List<JobPost> FindHirePosts(
        //    Profile profile,

        //    //JobCategory category = JobCategory.SoftwareDev
        //    )
        //{


        //    return new List<JobPost>();
        //}

        //public static List<JobSearchPost> FindLookingPosts(Profile profile)
        //{

        //}
    }
}
