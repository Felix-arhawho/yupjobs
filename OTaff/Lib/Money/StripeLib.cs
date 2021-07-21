using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static partial class StripeController
    {
        static StripeController()
        {
            //StripeConfiguration.ApiKey = "sk_test_51HfgfpItUMuYVtWkTIn4tc7L5j2ScHmxFwbNu7W6o8s95NHGg1OvaRWvLm4qodYOkoF59DZRRxLVdDg05Y36EKWh00TU30w588";
        }
        public static IStripeClient StripeClientIN = new StripeClient(
            apiKey: "sk_test_51IH5CUD3TwtgOYbKajmiPpqcNo6ZDTkf3YaNy1rNBf3Sv529SP3Tu9wGPlHSC6bqQTF7iwSdv3saYW4a3joIZMF700g5GF6har");

        public static InvoiceService InvoiceServiceEU = new InvoiceService();
        public static InvoiceService InvoiceServiceIN = new InvoiceService(StripeClientIN);

        //public static InvoiceItemService InvoiceItemService = new InvoiceItemService();
        public static CustomerService CustomerServiceIN = new CustomerService(StripeClientIN);
        public static CustomerService CustomerServiceEU = new CustomerService();

        public static PaymentMethodService PaymentMethodService = new PaymentMethodService();
        //private static ChargeService chargeService = new ChargeService();

        public static PaymentIntentService IntentServiceEU = new PaymentIntentService();
        public static PaymentIntentService IntentServiceIN = new PaymentIntentService(StripeClientIN);
        public static PaymentIntentService IntentServiceNA = new PaymentIntentService();


        public static InvoiceItemService ItemServiceEU = new InvoiceItemService();
        public static InvoiceItemService ItemServiceIN = new InvoiceItemService(StripeClientIN);
        public static InvoiceItemService ItemServiceNA = new InvoiceItemService();

        public static object CustomerLock = new object();
        public static object CustomerCreateLock = new object();
        public static object InvoiceLock = new object();
        public static object PaymentMethodLock = new object();

        public static Customer CreateCustomer(User user, Profile profile, IClientSessionHandle s)
        {
            try
            {
                var options = new CustomerCreateOptions
                {
                    Description = "UserId: " + user.Id,
                    Email = user.Email,
                    Name = profile.BusinessType is BusinessType.individual ? profile.FirstName + " " + profile.LastName : profile.OrgName,
                    Phone = user?.Phone,
                };
                
                Customer cus;
                //lock (CustomerCreateLock)
                QueueCtl.WaitForTurn();
                cus = CustomerServiceEU.Create(options);

                var upd = new UpdateDefinitionBuilder<User>().Set(x => x.StripeCustomerId, cus.Id);
                Db.UsersCollection.UpdateOne(s, x => x.Id == user.Id, upd);
                return cus;
            }
            catch
            {
                return null;
            }
        }


        //private static InvoiceService invoiceService = new InvoiceService();

        //public static KeyValuePair<Charge, Invoice> CreateChargeInvoice(
        //    User user, 
        //    DateTime dueDate, 
        //    List<KeyValuePair<string, decimal>> items, 
        //    string description = null)
        //{
        //    var options = new InvoiceCreateOptions()
        //    {
        //        CollectionMethod = "charge_automatically",
        //        Customer = user.StripeCustomerId,
        //        DueDate = DateTime.UtcNow+TimeSpan.FromDays(2),
        //        AutoAdvance = true,
        //    };

        //    Invoice invoice;
        //    lock (InvoiceLock) invoice = invoiceService.Create(options);
        //}

        private static PaymentIntent CreatePaymentIntent(PaymentIntentCreateOptions options, Currency cur)
        {


            QueueCtl.WaitForTurn();    
            return IntentServiceEU.Create(options);
        }

        public static async Task CreateP24Method(User user)
        {
            if (Db.PaymentMethodsCollection.CountDocuments(x => x.UserId == user.Id && x.Type == MethodType.Przelewy24) > 0)
                return;

            var authorizedCountries = new CountryCode[] {
                CountryCode.PL
            };

            if (!authorizedCountries.Contains(user.Country)) return;

            var options = new PaymentMethodCreateOptions()
            {
                P24 = new PaymentMethodP24Options()
                {
                    //Country = user.Country.GetName()
                },
                Type = "p24"
            };

            PaymentMethod method;
            //lock (PaymentMethodLock) 
            QueueCtl.WaitForTurn();    
            method = PaymentMethodService.Create(options);
            //lock (PaymentMethodLock)
            QueueCtl.WaitForTurn();
            method = PaymentMethodService.Attach(method.Id, new PaymentMethodAttachOptions()
            {
                Customer = user.StripeCustomerId,
            });

            var dbm = new UserPaymentMethod()
            {
                MethodId = method.Id,
                Type = MethodType.Przelewy24,
                MetaName = "Przelewy24",
                UserId = user.Id,
                Default = true,
            };

            Db.PaymentMethodsCollection.InsertOne(dbm);
        }

        public static async Task CreateSofortMethod(User user)
        {
            if (Db.PaymentMethodsCollection.CountDocuments(x => x.UserId == user.Id && x.Type == MethodType.Sofort) > 0)
                return;

            var authorizedCountries = new CountryCode[] { 
                CountryCode.AT,
                CountryCode.BE,
                CountryCode.DE,
                CountryCode.ES,
                CountryCode.IT,
                CountryCode.NL,
                //CountryCode.FR,
            };
            //AT, BE, DE, ES, IT, or NL

            if (!authorizedCountries.Contains(user.Country)) return;

            var options = new PaymentMethodCreateOptions() {
                Sofort = new PaymentMethodSofortOptions()
                {
                    Country = user.Country.GetName()
                },
                Type = "sofort"
            };

            PaymentMethod method;
            //lock (PaymentMethodLock) 
            QueueCtl.WaitForTurn();
            method = PaymentMethodService.Create(options);
            //lock (PaymentMethodLock) method = PaymentMethodService.Attach(method.Id, new PaymentMethodAttachOptions()
            //{
            //    Customer = user.StripeCustomerId
            //});

            var dbm = new UserPaymentMethod()
            {
                MethodId = method.Id,
                Type = MethodType.Sofort,
                MetaName = "Sofort",
                UserId = user.Id,
                Default = true,
            };

            Db.PaymentMethodsCollection.InsertOne(dbm);
        }        

        public static async Task<Dictionary<string, dynamic>> CreateLinkInvoice(
            User user,
            DateTime dueDate,
            Currency currency,
            decimal amount,
            decimal[] fees,
            string description,
            UserBill bill = null,
            BillActionData action = null
            )
        {
            //TODO INCREASE FEES ON FAILED BILLS

            try
            {
                using var s = await Db.Client.StartSessionAsync();

                var newBill = bill is null;

                Customer customer;
                //lock (CustomerLock) 
                QueueCtl.WaitForTurn();
                //if (currency is Currency.INR)
                //    customer = CustomerServiceIN.Create(new CustomerCreateOptions()
                //    {
                //        Email = user.Email,
                //    });
                //else
                    customer = CustomerServiceEU.Create(new CustomerCreateOptions()
                    {
                        Email = user.Email,
                    });

                var options = new InvoiceCreateOptions()
                {
                    CollectionMethod = "send_invoice",
                    Customer = customer.Id,
                    DueDate = dueDate,
                    AutoAdvance = false,
                    Description = description,
                    Footer = "YupJobs",
                    //DaysUntilDue = 31,
                };

                var amt = /*currency is Currency.INR ? (long)(amount * 100) :*/ amount.GetCents(currency);
                if (description != null) options.StatementDescriptor = new string(description.Take(22).ToArray());

                var otp = new InvoiceItemCreateOptions()
                {
                    //Description = new string(description.Take(22).ToArray()),
                    Amount = amt,
                    //Invoice = invoice.Id,
                    Currency = Enum.GetName(typeof(Currency), currency).ToLower(),
                    Customer = customer.Id,
                };
                //lock (InvoiceLock) 
                QueueCtl.WaitForTurn();

                //if (currency is Currency.INR)
                //    ItemServiceIN.Create(otp);
                //else 
                    ItemServiceEU.Create(otp);

                Invoice invoice;
                //lock (InvoiceLock) 
                QueueCtl.WaitForTurn();

                //if (currency is Currency.INR)
                //    invoice = InvoiceServiceIN.Create(options);
                //else
                    invoice = InvoiceServiceEU.Create(options);

                bill = bill ?? new UserBill()
                {
                    UserId = user.Id,
                    DateIssued = DateTime.UtcNow,
                    Description = description,
                    Action = action is null ? BillAction.None : action.ActionType,
                    AutoCharged = false,
                    Framework = PaymentFramework.Invoice,
                    MethodType = MethodType.StripeInvoice,
                    TotalAmountRequested = amount,
                    NextVerif = DateTime.UtcNow.AddMinutes(2),
                    VerifInterval = TimeSpan.FromMinutes(5),
                    StripeInvoiceId = invoice.Id,
                    Currency = currency,
                    TotalFees = fees,
                    //InvoiceUrl = invoice.HostedInvoiceUrl
                };

                s.StartTransaction();

                if (newBill)
                {
                    Db.UserBillsCollection.InsertOne(s, bill);
                    if (action != null)
                    {
                        action.BillId = bill.Id;
                        Db.BillActions.InsertOne(s, action);
                    }
                }
                else Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.StripeInvoiceId, invoice.Id)
                    .Set(x => x.Framework, PaymentFramework.Invoice)
                    .Set(x => x.NextVerif, DateTime.UtcNow.AddMinutes(2))
                    .Set(x => x.VerifInterval, TimeSpan.FromMinutes(5)));
                //.Set(x => x.InvoiceUrl, invoice.HostedInvoiceUrl));

                //lock (InvoiceLock) 
                QueueCtl.WaitForTurn();

                //if (currency is Currency.INR)
                //    invoice = InvoiceServiceIN.FinalizeInvoice(invoice.Id);
                //else
                    invoice = InvoiceServiceEU.FinalizeInvoice(invoice.Id);


                Db.UserBillsCollection.UpdateOne(s, x => x.Id == bill.Id, new UpdateDefinitionBuilder<UserBill>()
                    .Set(x => x.InvoiceUrl, invoice.HostedInvoiceUrl));
                
                s.CommitTransaction();
                return new Dictionary<string, dynamic>()
                {
                    {"redirect", false },
                    {"newtab", true },
                    {"bill", bill },
                    {"url", invoice.HostedInvoiceUrl }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, dynamic>() 
                {
                    {"error", e.ToString() }
                };
            }
        }

        public static PaymentMethod AddPaymentCard(PaymentMethodCreateOptions options)
        {
            PaymentMethod method;
            var cus = options.Customer;
            options.Customer = null;

            QueueCtl.WaitForTurn();
            method = PaymentMethodService.Create(options);

            QueueCtl.WaitForTurn(); 
            return PaymentMethodService.Attach(method.Id, new PaymentMethodAttachOptions
            {
                Customer = cus,
            });
        }

        public static PaymentMethod CreatePaymentMethod(PaymentMethodCreateOptions options)
        {
            PaymentMethod method;
            var cus = options.Customer;
            options.Customer = null;

            QueueCtl.WaitForTurn(); 
            method = PaymentMethodService.Create(options);
            QueueCtl.WaitForTurn(); 
            return PaymentMethodService.Attach(method.Id, new PaymentMethodAttachOptions
            {
                Customer = cus,
            });
        }


        public static Customer GetCustomer(string id)
        {
            try
            {
                lock (CustomerLock) return CustomerServiceEU.Get(id);
            }
            catch
            {
                return null;
            }
        } 

        public static bool VoidInvoice()
        {
            return true;
        }
    }
}
