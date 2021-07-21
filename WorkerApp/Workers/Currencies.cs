//using SharedLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace WorkerApp.Workers
//{
//    public static Task RatesWorker()
//    {
//        Rates[Currency.EUR] = 1;
//        Console.WriteLine("[WORKER] Started currency convertor");
//        while (true)
//        {
//            try
//            {
//                string pms = "?base=EUR&symbols=";
//                var cnt = 0;
//                var tls = Enum.GetNames(typeof(Currency)).ToList();
//                tls.Remove("EUR");


//                foreach (var i in tls)
//                {
//                    cnt++;
//                    if (cnt == tls.Count) pms += $"{i}";
//                    else pms += $"{i},";
//                }
//                pms += "&access_key=25182b0873e4447e94c536fabc15a9e2";

//                var obj = JObject.Parse(Client.GetAsync(Url + pms).Result.Content.ReadAsStringAsync().Result);

//                foreach (var i in tls)
//                {
//                    Rates[Enum.Parse<Currency>(i)] = obj["rates"].Value<decimal>(i);
//                }

//                Console.WriteLine("EUR BASE");
//                foreach (var i in Enum.GetValues(typeof(Currency)))
//                    Console.WriteLine($"{Enum.GetName(typeof(Currency), i)} => {Rates[(Currency)i]}");

//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//            }

//            Task.Delay(1000 * 60 * 10).Wait();
//        }
//    }
//}
