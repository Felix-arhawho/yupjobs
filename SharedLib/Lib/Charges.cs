//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using SharedLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;

//namespace SharedLib.Lib
//{
//    public class ChargeHandle
//    {
//        public HandleType HandleType { get; set; }

//        //for direct response
//        public bool Paid { get; set; }

//        // for redirects
//        public string RedirectUrl { get; set; }
//        public string ConfirmUrl { get; set; }

//        // for failed

//        // for confirms
//        public decimal Total { get; set; }

//        public UserBill Bill { get; set; }

//        public async Task<Dictionary<string, dynamic>> ConfirmBill()
//        {
//            var resp = await Ez.GetHttpPostResponse(
//                "/api/billing/confirmbill", 
//                new Dictionary<string, string>() 
//                {
//                    {"billId", Bill.Id }
//                });
//            if (resp.IsSuccessStatusCode)
//                return JObject.Parse(await resp.Content.ReadAsStringAsync()).ToObject<Dictionary<string, dynamic>>();           
//            else return default;
//        }

//        public static async Task<ChargeHandle> ParseResponse(HttpResponseMessage response, bool dispose = false)
//        {
//            var resobj = JObject.Parse(await response.Content.ReadAsStringAsync());
//            var handle = new ChargeHandle();

//            handle.Bill = resobj["bill"].ToObject<UserBill>();
//            if (resobj.Value<bool>("redirect"))
//            {
//                handle.HandleType = HandleType.Redirect;
//                handle.Paid = false;
//                handle.RedirectUrl = resobj.Value<string>("url");
//            }
//            else if (resobj.Value<bool>("paid"))
//            {
//                handle.HandleType = HandleType.Direct;
//                handle.Paid = true;
//            }
//            else if (resobj.TryGetValue("newtab", out var val))
//            {
//                if (val.Value<bool>())
//                {
//                    handle.HandleType = HandleType.NewTab;
//                    handle.Paid = false;
//                    handle.RedirectUrl = resobj.Value<string>("url");
//                } 
//            }
//            else if (resobj.TryGetValue("delayed", out val))
//            {
//                if (val.Value<bool>())
//                {
//                    handle.HandleType = HandleType.DelayedNotif;
//                    handle.Paid = false;
//                }
//            }

//            if (dispose) response.Dispose();

//            return handle;
//        }
//    }

//    public enum HandleType
//    {
//        Redirect,
//        DelayedNotif,
//        NewTab,
//        Direct,
//        Fail,
//    } 
//}
