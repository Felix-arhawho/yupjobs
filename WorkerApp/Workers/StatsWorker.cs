using MongoDB.Driver;
using ServerLib;
using SharedLib.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace WorkerApp.Workers
{
    public static class StatsWorker
    {
        public static async Task MoneyTask()
        {
            await Task.Delay(1000 * 30);
            Console.WriteLine($"[WORKER][STATS] Started Money stats worker");
            while (true)
            {
                try
                {
                    var wallets = Db.UserWalletsCollection.Find(x => !x.Hidden).ToList();

                    var eurTotal = 0m;
                    foreach (var i in wallets)
                    {
                        eurTotal += FeesCtl.ConvertToEur(i.Currency, i.Funds);
                    }

                    Console.WriteLine($"[WORKER][STATS] Total amount in wallets is of {eurTotal} €");
                    wallets.Clear();
                    _ = Task.Run(delegate
                    {
                        var dt = DateTime.UtcNow;
                        bool run = true;
                        var timer = new Timer();
                        timer.Interval = 1000 * 60 * 59;
                        timer.Elapsed += new ElapsedEventHandler((s, s1) => run = false);
                        timer.Start();
                        while (run)
                        {
                            Console.WriteLine($"[WORKER][STATS] Total amount in wallets is of {eurTotal} € as of {dt.ToShortTimeString()}");
                            Task.Delay(1000 * 60).Wait();
                        }
                    });
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
                await Task.Delay(1000 * 60 * 60);
            }
        }
    }
}
