using MongoDB.Driver;
using Newtonsoft.Json;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Lib;
using SharedLib.Models;
using Spectre.Console;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
//using static System.Console;
using static Spectre.Console.AnsiConsole;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            ClearDb();
            return;
            try
            {
                var usrs = Db.UsersCollection.Find(x => true).ToList();
                WriteLine(JsonConvert.SerializeObject(usrs, Formatting.Indented));

            }catch(Exception e)
            {
                WriteException(e);
            }

            return;

            var tok = Ask<string>("Token").ToObject<Jwt>();
            if (tok.VerifyMultiJwt().Result)
                WriteLine("VALID");
            else WriteLine("INVALID");


            
            WriteLine("Finished job");
            Main(null);
            //ReadKey();
            //server.Abort();
            Environment.Exit(0);
        }

        static void ClearDb()
        {
            WriteLine("Starting DB Flush");
            var tls = new Task[] {
                Db.ArchivedJobsCollection.DeleteManyAsync(x=>true),
                Db.BillActions.DeleteManyAsync(x => true),
                Db.ConnectAccounts.DeleteManyAsync(x => true),
                Db.ConversationsCollection.DeleteManyAsync(x => true),
                //Db.CustomerInvoicesCollection.DeleteManyAsync(x => true),
                Db.FeesCollection.DeleteManyAsync(x => true),
                Db.JobAppliesCollection.DeleteManyAsync(x => true),
                Db.JobPaymentsArchive.DeleteManyAsync(x => true),
                Db.JobPaymentsCollection.DeleteManyAsync(x => true),
                Db.JobPostsCollection.DeleteManyAsync(x => true),
                Db.JobSearchPostsCollection.DeleteManyAsync(x => true),
                Db.JwtsCollection.DeleteManyAsync(x => true),
                Db.MessagesCollection.DeleteManyAsync(x => true),
                Db.OngoingJobsCollection.DeleteManyAsync(x => true),
                Db.OtpCollection.DeleteManyAsync(x => true),
                Db.PaymentMethodsCollection.DeleteManyAsync(x => true),
                Db.ProfilesCollection.DeleteManyAsync(x => true),
                Db.ReportsCollection.DeleteManyAsync(x => true),
                Db.SearchProposalsCollection.DeleteManyAsync(x => true),
                Db.SubBillActions.DeleteManyAsync(x => true),
                Db.SubscriptionsCollection.DeleteManyAsync(x => true),
                Db.UserBillsCollection.DeleteManyAsync(x => true),
                Db.UserMediaCollection.DeleteManyAsync(x => true),
                Db.UsersCollection.DeleteManyAsync(x => true),
                Db.UserWalletsCollection.DeleteManyAsync(x => true),
                Db.WalletBillActions.DeleteManyAsync(x => true),
                Db.WalletTransactionsArchives.DeleteManyAsync(x => true),
                Db.WalletTransactionsCollection.DeleteManyAsync(x => true),
                Db.TransactionActions.DeleteManyAsync(x=>true),
                Db.SupportTickets.DeleteManyAsync(x=>true),
                Db.TicketMessages.DeleteManyAsync(x=>true),
            };

            Task.WhenAll(tls).Wait();

            WriteLine("Finished flushing DB");
        }

        static List<string> Func()
        {
            return default;
        }
    }
}
