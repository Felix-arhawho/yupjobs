using Newtonsoft.Json;
using SharedLib.Lib;
using SharedLib.Models;
using BlazorApp.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorApp.Shared;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Lib
{
    public enum ColorTheme
    {
        Light,
        Dark
    }

    public static partial class Session
    {
        public static bool LoggedIn = false;
        public static bool GotInfo = false;
        public static ColorTheme Theme = ColorTheme.Light;
        
        public static Jwt Token { get => Ez.AuthToken; set => Ez.AuthToken = value; }

        public static User User = new User() 
        { 
            DefaultCurrency = Currency.EUR
        };
        public static Profile Profile = null;
        public static UserSubscription Subscription = null;

        public static UserConnectAccount ConnectAccount = null;

        public static Dictionary<Currency, decimal> CurrencyRates { get => Ez.CurrencyRates; set => Ez.CurrencyRates = value; }

        public static List<Conversation> Conversations = new List<Conversation>();
        public static List<ChatMessage> ChatMessages = new List<ChatMessage>();

        public static List<UserPaymentMethod> UserPaymentMethods = new List<UserPaymentMethod>();
        public static List<Notification> Notifications = new List<Notification>();
        public static List<UserWallet> UserWallets = new List<UserWallet>();
        
        public static List<JobApply> MyJobApplications = new List<JobApply>();

        public static List<Job> Jobs = new List<Job>();

        public static List<JobPost> CurrentJobPosts = new List<JobPost>();
        public static List<JobSearchPost> CurrentJobSearchPosts = new List<JobSearchPost>();

        public static List<SupportTicket> SupportTickets = new List<SupportTicket>();
        public static List<TicketMessage> TicketMessages = new List<TicketMessage>();

        public static void Logout()
        {
            LoggedIn = false;
            GotInfo = false;

            Token = null;
            User = null;
            Profile = null;
            Subscription = null;
            ConnectAccount = null;

            CurrencyRates.Clear();
            Conversations.Clear();
            ChatMessages.Clear();
            UserPaymentMethods.Clear();
            Notifications.Clear();
            UserWallets.Clear();
            MyJobApplications.Clear();
            Jobs.Clear();
        }
    }

    public static class Workers
    {
        //[Inject] Blazored.LocalStorage.ISyncLocalStorageService StorageService { get; set; }

        public static bool SessionWorkerStarted = false;

        public static async Task GetInfo()
        {
            using var resp = await Ez.GetHttpPostResponse("home/sessiondata");
            if (resp.IsSuccessStatusCode)
            {
                var stre = await resp.Content.ReadAsStringAsync();
                //StorageService.SetItem("sessiondata", stre);

                var jobj = JObject.Parse(stre);
                //Debug.WriteLine(jobj.ToString());

                Session.User = jobj["user"].ToObject<User>();
                Session.Profile = jobj["profile"].ToObject<Profile>();
                Session.Subscription = jobj["subscription"].ToObject<UserSubscription>();
                Session.CurrencyRates = jobj["currencies"].ToObject<Dictionary<Currency, decimal>>();
                Session.Notifications = jobj["notifications"].ToObject<List<Notification>>();
                Session.ConnectAccount = jobj["connect"].ToObject<UserConnectAccount>();
                Session.UserWallets = jobj["wallets"].ToObject<List<UserWallet>>();
                Session.UserPaymentMethods = jobj["methods"].ToObject<List<UserPaymentMethod>>();
                Session.Jobs = jobj["jobs"].ToObject<List<Job>>();

                Session.GotInfo = true;
            }
            else if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized)
            {
                Session.LoggedIn = false;
                Session.Logout();
            }
        }

        public static async Task GetSessionInfo()
        {
            Debug.WriteLine($"Started session ino task");

            if (SessionWorkerStarted) return;
            SessionWorkerStarted = true;

            while (SessionWorkerStarted)
            {
                try
                {
                    await GetInfo();
                    //Debug.Write("NOW POSTING TO home/sessiondata");
                    //using var resp = await Ez.GetHttpPostResponse("home/sessiondata");
                    //if (resp.IsSuccessStatusCode)
                    //{
                    //    var stre = await resp.Content.ReadAsStringAsync();
                    //    //StorageService.SetItem("sessiondata", stre);

                    //    var jobj = JObject.Parse(stre);
                    //    //Debug.WriteLine(jobj.ToString());

                    //    Session.User = jobj["user"].ToObject<User>();
                    //    Session.Profile = jobj["profile"].ToObject<Profile>();
                    //    Session.Subscription = jobj["subscription"].ToObject<UserSubscription>();
                    //    Session.CurrencyRates = jobj["currencies"].ToObject<Dictionary<Currency, decimal>>();
                    //    Session.Notifications = jobj["notifications"].ToObject<List<Notification>>();
                    //    Session.ConnectAccount = jobj["connect"].ToObject<UserConnectAccount>();
                    //    Session.UserWallets = jobj["wallets"].ToObject<List<UserWallet>>();
                    //    Session.UserPaymentMethods = jobj["methods"].ToObject<List<UserPaymentMethod>>();
                    //    Session.Jobs = jobj["jobs"].ToObject<List<Job>>();

                    //    Session.GotInfo = true;
                    //}
                    //else if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized)
                    //{
                    //    Session.LoggedIn = false;
                    //    Session.Logout();
                    //}
                }
                catch(Exception e)
                {
                    Debug.WriteLine($"ERROR HAS OCCURED: \n\n {e}");
                }
                finally
                {

                }
                
                await Task.Delay(1000 * 60);
            }

        }


    }
}
