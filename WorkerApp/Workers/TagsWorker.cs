using MongoDB.Driver;
using OTaff.Lib;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkerApp.Workers
{
    public static class TagsWorker
    {
        public static bool ForceStop = false;

        public static async Task DoWork()
        {
            while (!ForceStop)
            {
                var s = Db.Client.StartSession();
                try
                {
                    Console.WriteLine("[WORKER][TAGS] New round started");

                    
                    var tagsls = new List<string>();
                    var cursor1 = Db.JobPostsCollection.Find(x => true).ToList();
                    var cursor2 = Db.JobSearchPostsCollection.Find(x => true).ToList();

                    foreach (var i in cursor1)
                        foreach (var i2 in i.Tags)
                            if (!tagsls.Contains(i2))
                                tagsls.Add(i2);
                    foreach (var i in cursor2)
                        foreach (var i2 in i.Tags)
                            if (!tagsls.Contains(i2))
                                tagsls.Add(i2);
                    var dbtags = new List<DbTag>();

                    s.StartTransaction();
                    Db.TagMetaCollection.DeleteMany(s, x => true);
                    foreach (var i in tagsls)
                    {
                        dbtags.Add(new DbTag()
                        {
                            TagName = i,
                            UseCount = await Task.Run(() => {
                                var cnt1 = cursor1.Count(x => x.Tags.Contains(i));
                                var cnt2 = cursor2.Count(x => x.Tags.Contains(i));
                                return cnt1 + cnt2;
                            })
                        });
                    }

                    Db.TagMetasLocalCollection = dbtags;
                    Db.TagMetaCollection.InsertMany(s, dbtags);
                    s.CommitTransaction();

                    await Task.Delay(1000 * 60 * 60);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    s.Dispose();
                }
            }
        }
    }
}
