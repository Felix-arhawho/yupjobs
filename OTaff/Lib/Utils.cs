using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using MongoDB.Driver;
using SharedLib.Models;
using ServerLib;

namespace OTaff.Lib
{
    public static class QueueCtl
    {
        static QueueCtl()
        {
            Task.Factory.StartNew(StripeDequeue, TaskCreationOptions.LongRunning);
        }

        public static ConcurrentQueue<Guid> OngoingStripeRequests = new ConcurrentQueue<Guid>();
        static object Lock = new object();

        public static void WaitForTurn()
        {
            //var time = DateTime.UtcNow;
            var reqId = Guid.NewGuid();

            //var item = new ValueTuple<Guid, DateTime>(reqId, time);

            OngoingStripeRequests.Enqueue(reqId);

            while (OngoingStripeRequests.Contains(reqId)) Task.Delay(30).Wait();
        }

        private static async Task StripeDequeue()
        {
            Console.WriteLine("[WORKER] Started stripe requests queue system");
            while (true)
            {
                if (OngoingStripeRequests.Count is 0) await Task.Delay(100);
                else
                {
                    int cnt = 0;
                    for (int i = 0; i < 80; i++)
                    {
                        if (OngoingStripeRequests.TryDequeue(out var id))
                            cnt++;
                    }
                    await Task.Delay(980);
                    //Console.WriteLine($"Released {cnt} requests this round");
                }
            }
        }

        //public static void EndStripeRequest(Guid id)
        //{
        //    OngoingStripeRequests.RemoveAll(x => x.Item1 == id);
        //}
    }

    public static class SearchCtl
    {
        public static FilterDefinition<JobPost> SearchFilter(JobPostFilter filter)
        {
            bool TagSearch = true;
            if (filter.Categories[0] is JobCategory.Any)
                foreach (var i in Enum.GetValues(typeof(JobCategory))) filter.Categories.Add((JobCategory)i);
            if (filter.Tags.Count is 0)
                TagSearch = false;
                //filter.Tags = Db.TagMetasLocalCollection.OrderByDescending(x => x.UseCount).Take(80).Select(x=>x.TagName).ToList();
            if (string.IsNullOrWhiteSpace(filter.Search)) filter.Search = string.Empty;
            if (filter.MaxDate > DateTime.UtcNow || filter.MaxDate < DateTime.UtcNow - TimeSpan.FromDays(60))
                filter.MaxDate = DateTime.UtcNow - TimeSpan.FromDays(36);

            //TagSearch = false;

            if (TagSearch)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Categories.Any(c => filter.Categories.Contains(c))
                    && (x.Title.ToLower().Contains(filter.Search.ToLower()) || x.Description.ToLower().Contains(filter.Search.ToLower()))
                    && x.Tags.Any(t => filter.Tags.Contains(t))
                    && x.PostDate >= filter.MaxDate);
            else return new ExpressionFilterDefinition<JobPost>(
                    x => x.Categories.Any(c => filter.Categories.Contains(c))
                    && (x.Title.ToLower().Contains(filter.Search.ToLower()) || x.Description.ToLower().Contains(filter.Search.ToLower()))
                    && x.PostDate >= filter.MaxDate);




            //TODO

            var catSrc = filter.Categories.Count >= 1 && filter.Categories.First() != JobCategory.Any;
            var tagSrc = filter.Tags.Count > 0;
            var txtSrc = !string.IsNullOrWhiteSpace(filter.Search);

            //var dbFilter = new ExpressionFilterDefinition<JobPost>(x =>
            //            x.PostDate >= filter.MaxDate &&
            //            catSrc ? filter.Categories.Any(g=>x.Categories.Contains(g)) : true
            //            && txtSrc ? (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search)) : true
            //            && tagSrc ? filter.Tags.Any(t => x.Tags.Contains(t)) : true
            //            /*&& filter.MinimumSalary >= x.ProposedSalary[0]*/);

            if (catSrc && tagSrc && txtSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Categories.Any(c => filter.Categories.Contains(c))
                    && (x.Title.ToLower().Contains(filter.Search.ToLower()) || x.Description.ToLower().Contains(filter.Search.ToLower()))
                    && x.Tags.Any(t => filter.Tags.Contains(t))
                    && x.PostDate >= filter.MaxDate);
            else if (tagSrc && txtSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Tags.Any(t => filter.Tags.Contains(t))
                    && x.PostDate >= filter.MaxDate
                    && (x.Title.ToLower().Contains(filter.Search) || x.Description.ToLower().Contains(filter.Search)));
            else if (tagSrc && catSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Categories.Any(c => filter.Categories.Contains(c))
                    //&& (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search))
                    && x.Tags.Any(t => filter.Tags.Contains(t)));
            else if (catSrc && txtSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Categories.Any(c => filter.Categories.Contains(c))
                    && (x.Title.ToLower().Contains(filter.Search) || x.Description.ToLower().Contains(filter.Search)));
            else if (catSrc && tagSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => x.Tags.Any(t => filter.Tags.Contains(t))
                    && x.Categories.Any(c => filter.Categories.Contains(c)));
            else if (txtSrc)
                return new ExpressionFilterDefinition<JobPost>(
                    x => (x.Title.ToLower().Contains(filter.Search) || x.Description.ToLower().Contains(filter.Search)));
            else if (tagSrc)
                return new ExpressionFilterDefinition<JobPost>(x => x.Tags.Any(t => filter.Tags.Contains(t)));
            else if (catSrc)
                return new ExpressionFilterDefinition<JobPost>(x => x.Categories.Any(t => filter.Categories.Contains(t)));
            else return FilterDefinition<JobPost>.Empty;
            


            //dbFilter = dbFilter.Expression.Reduce();

            //if (filter.Equals(new JobPostFilter()))
            //{
            //    return new ExpressionFilterDefinition<JobPost>(x => x.Active && x.PostDate >= filter.MaxDate && x.ProposedSalary[0] > filter.MinimumSalary);
            //}
            //if (string.IsNullOrWhiteSpace(filter.Search)) filter.Search = string.Empty;
            //if (filter.Tags.Count is 0)
            //{
            //    return new ExpressionFilterDefinition<JobPost>(x =>
            //        x.PostDate >= filter.MaxDate
            //        && (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search))
            //        && x.Categories.Any(g=>filter.Categories.Contains(g))
            //        && filter.MinimumSalary <= x.ProposedSalary[0]);
            //}
            //else if (
            //    filter.Categories.First() is JobCategory.Any 
            //    && filter.Categories.Count is 1)
            //{
            //    return new ExpressionFilterDefinition<JobPost>(x =>
            //        x.PostDate >= filter.MaxDate
            //        && filter.Categories.Any(c => x.Categories.Contains(c))
            //        && (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search))
            //        && filter.MinimumSalary <= x.ProposedSalary[0]);
            //}
            //else if (
            //    filter.Tags.Count is 0 
            //    && filter.Categories.First() is JobCategory.Any 
            //    && filter.Categories.Count is 1)
            //{
            //    return new ExpressionFilterDefinition<JobPost>(x =>
            //                x.PostDate >= filter.MaxDate
            //                && (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search))
            //                && filter.MinimumSalary <= x.ProposedSalary[0]);
            //}
            //else
            //{
            //    return new ExpressionFilterDefinition<JobPost>(x =>
            //                x.PostDate >= filter.MaxDate
            //                && filter.Categories.Any(c => x.Categories.Contains(c))
            //                && (x.Title.Contains(filter.Search) || x.Description.Contains(filter.Search))
            //                && filter.Tags.Any(t => x.Tags.Contains(t))
            //                && filter.MinimumSalary <= x.ProposedSalary[0]);
            //}

            return FilterDefinition<JobPost>.Empty;
        }
    }
}
