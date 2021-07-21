using MongoDB.Bson.Serialization.Attributes;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class StripeAccounts
    {
        static StripeAccounts()
        {
            //StripeConfiguration.ApiKey = "sk_test_51HfgfpItUMuYVtWkTIn4tc7L5j2ScHmxFwbNu7W6o8s95NHGg1OvaRWvLm4qodYOkoF59DZRRxLVdDg05Y36EKWh00TU30w588";
        }

        public static object ConnectLock = new object();
        public static AccountService AccountServiceIN = new AccountService();
        public static AccountService AccountServiceEU = new AccountService();
        public static TransferService TransferService = new TransferService();
        public static PayoutService PayoutService = new PayoutService();
        public static TokenService TokenService = new TokenService();

        public static async Task TransferToAccount()
        {
            //prendre carte

            //faire payer

            //si demande de fdp => envoyer le url
            //sinon dire c bon et marquer comme payé

            //pour demande url, fonction de confirmation api => qui dit que c payé
        }

        public static async Task PayoutAccount()
        {

        }

        /// <summary>
        /// Pay 5 CUR to platform, then get credited 5 CUR on first payment
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static UserConnectAccount CreateConnectAccount(User user)
        {
            //using var s = Db.Client.StartSessionAsync();
            

            var options = new AccountCreateOptions
            {
                //Type = "custom",
                Type = "standard",
                Country = Enum.GetName(typeof(CountryCode), user.Country),
                BusinessType = Enum.GetName(typeof(BusinessType), user.BusinessType).ToLower(),
                //DefaultCurrency = Enum.GetName(typeof(Currency), user.DefaultCurrency).ToLower(),
                //AccountToken = token.Id,
                //Company = new AccountCompanyOptions() { 
                //    Name = details.OrgName,
                //    Address = new AddressOptions()
                //    {
                //        City = details.Address.City,
                //        Country = details.Address.Country,
                //        Line1 = details.Address.Line1,
                //        Line2 = details.Address.Line2,
                //        PostalCode = details.Address.PostalCode,
                //        State = details.Address.State
                //    },
                //    g
                //},

                Email = user.Email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    //CardPayments = new AccountCapabilitiesCardPaymentsOptions
                    //{
                    //    Requested = true,
                    //},
                    //Transfers = new AccountCapabilitiesTransfersOptions
                    //{
                    //    Requested = true,
                    //},
                    //SepaDebitPayments = new AccountCapabilitiesSepaDebitPaymentsOptions
                    //{
                    //    Requested = true,
                    //}
                },
            };

            //s.StartTransaction();

            Account nacc;

            QueueCtl.WaitForTurn();
            if (user.Country is CountryCode.IN)
                nacc = AccountServiceIN.Create(options);
            else nacc = AccountServiceEU.Create(options);

            var acc = new UserConnectAccount()
            {
                RegistrationCurrency = user.DefaultCurrency,
                UserId = user.Id,
                StripeCustomerId = user.StripeCustomerId,
                ConnectId = nacc.Id
            };

            Db.ConnectAccounts.InsertOne(acc);

            return acc;
        }

        public static AccountLinkService LinkServiceEU = new AccountLinkService();
        public static AccountLinkService LinkServiceIN = new AccountLinkService();


        public static AccountLink GetAccountOnboardLink(string accId)
        {
            try
            {
                var options = new AccountLinkCreateOptions
                {
                    Account = accId,
                    RefreshUrl = "https://yupjobs.net/success",
                    ReturnUrl = "https://yupjobs.net/success",
                    Type = "account_onboarding",
                    Collect = "eventually_due"
                };

                QueueCtl.WaitForTurn();
                return LinkServiceEU.Create(options);
            }
            catch
            {
                return null;
            }
        }

        public static AccountLink GetAccountEditLink(string accId)
        {
            try
            {
                var options = new AccountLinkCreateOptions
                {
                    Account = accId,
                    RefreshUrl = "https://example.com/reauth",
                    ReturnUrl = "https://example.com/return",
                    Type = "account_update",
                    Collect = "eventually_due"
                };

                QueueCtl.WaitForTurn();
                return LinkServiceEU.Create(options);
            }
            catch
            {
                return null;
            }
        }
    }
}
