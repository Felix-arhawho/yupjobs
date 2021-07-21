using Newtonsoft.Json.Linq;
using SharedLib.Lib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class CurrencyConversion
    {
        static string Url = "http://api.exchangeratesapi.io/v1/latest";
        static string Url2 = "http://data.fixer.io/api/latest";

        private static HttpClient Client = new HttpClient();

        /// <summary>
        /// EUR base
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public static decimal GetConvertedRate(decimal amount, Currency currency, decimal fee = 0.02m, bool roundToHigher = false)
        {
            amount = amount + (amount * fee);
            var namt = amount * Rates[currency];
            if (roundToHigher)
                return Math.Round(namt, 0, MidpointRounding.ToPositiveInfinity);
            else return Math.Round(namt, 2, MidpointRounding.ToPositiveInfinity);
        }

        public static decimal ConvertToEur(Currency currency, decimal amount)
        {
            if (currency is Currency.EUR) return amount;

            var curVal = Rates[currency]; // the 1.85
            return Math.Round(amount / curVal, 2, MidpointRounding.ToPositiveInfinity);
        }

        public static Dictionary<Currency, decimal> Rates { get => Ez.CurrencyRates; set => Ez.CurrencyRates = value; } 
        //    = new Dictionary<Currency, decimal>()
        //{
        //    // EUR BASE
        //    {Currency.EUR, 1 },
            
        //    // CURRENCIES
        //    {Currency.GBP, 1 },
        //    {Currency.USD, 1.2m },
        //    {Currency.INR, 90 }
        //};

        public static Task RatesWorker()
        {
            Rates[Currency.EUR] = 1;
            Console.WriteLine("[WORKER] Started currency convertor");
            while (true)
            {
                try
                {
                    string pms = "?base=EUR&symbols=";
                    var cnt = 0;
                    var tls = Enum.GetNames(typeof(Currency)).ToList();
                    tls.Remove("EUR");


                    foreach (var i in tls)
                    {
                        cnt++;
                        if (cnt == tls.Count) pms += $"{i}";
                        else pms += $"{i},";
                    }
                    //pms += "";
                    var pms1 = "&access_key=25182b0873e4447e94c536fabc15a9e2";
                    var pms2 = "&access_key=a49a18c1f3401ab6ade770101c4ef841";
                    
                    try
                    {
                        var obj = JObject.Parse(Client.GetAsync(Url + pms + pms1).Result.Content.ReadAsStringAsync().Result);
                        foreach (var i in tls)
                        {
                            Rates[Enum.Parse<Currency>(i)] = obj["rates"].Value<decimal>(i);
                        }
                    }
                    catch
                    {
                        var obj = JObject.Parse(Client.GetAsync(Url2 + pms + pms2).Result.Content.ReadAsStringAsync().Result);
                        foreach (var i in tls)
                        {
                            Rates[Enum.Parse<Currency>(i)] = obj["rates"].Value<decimal>(i);
                        }

                    }

                    Console.WriteLine("EUR BASE");
                    foreach (var i in Enum.GetValues(typeof(Currency)))
                        Console.WriteLine($"{Enum.GetName(typeof(Currency), i)} => {Rates[(Currency)i]}");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Task.Delay(1000 * 60 * 60 * 3).Wait();
            }
        }
    }
}
