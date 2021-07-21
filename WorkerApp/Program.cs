using Stripe;
using System;
using System.Threading.Tasks;

namespace WorkerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            StripeConfiguration.ApiKey = "sk_test_51HfgfpItUMuYVtWkTIn4tc7L5j2ScHmxFwbNu7W6o8s95NHGg1OvaRWvLm4qodYOkoF59DZRRxLVdDg05Y36EKWh00TU30w588";

            Task.Run(async delegate 
            {
                Task.Run(OTaff.Lib.Money.CurrencyConversion.RatesWorker);
                Workers.BillWorker.Start = true;
                Workers.SubscriptionsWorker.Start = true;
                Workers.TransactionsWorker.Start = true;

                Workers.TransactionsWorker.DoWork();
                Workers.MediaCleanup.Cleanup();
                Workers.BillWorker.ActionWork();
                Workers.BillWorker.PayCheck();
                //Workers.TagsWorker.DoWork();
                Workers.StatsWorker.MoneyTask();
                Workers.IntervalPaymentsWorker.CreatePayments();
                Workers.DbCleaner.DbClean();
            });

            Task.Delay(-1).Wait();
        }
    }
}
