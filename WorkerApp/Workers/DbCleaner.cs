using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace WorkerApp.Workers
{
    public static class DbCleaner
    {
        public static Task DbClean()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("[WORKER][DB CLEANER] New round has started");
                    Db.JwtsCollection.DeleteMany(x => x.Issued < DateTime.UtcNow - TimeSpan.FromDays(61));
                    Db.FeesCollection.DeleteMany(x => x.Date < DateTime.UtcNow - TimeSpan.FromDays(390));
                    Db.BillActions.DeleteMany(x => x.Issued < DateTime.UtcNow - TimeSpan.FromDays(200));
                    Db.ConversationsCollection.DeleteMany(x => x.Created < DateTime.UtcNow - TimeSpan.FromDays(365));
                    Db.MessagesCollection.DeleteMany(x => x.DateSent < DateTime.UtcNow - TimeSpan.FromDays(365));


                    //24 Hours
                    Task.Delay(1000 * 60 * 60 * 24).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return Task.CompletedTask;
        }
    }
}
