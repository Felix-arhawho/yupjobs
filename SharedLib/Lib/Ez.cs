//#define LOCAL

using Newtonsoft.Json;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib.Lib
{
    public static class Ez
    {
#if LOCAL
        public static string Url = "http://localhost:15040/api/";
        public static string ApiUrl = "http://localhost:15040/api/";
        public static string ClientUrl = "http://localhost:5000/";
#else
        public static string Url = "https://www.yupjobs.net/api/";
        public static string ApiUrl = "https://api.yupjobs.net/api/";
        public static string ClientUrl = "https://www.yupjobs.net/";
#endif
        static Ez()
        {
            //ServicePointManager.ServerCertificateValidationCallback +=
            //    (sender, cert, chain, sslPolicyErrors) => true;

            //Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            //HttpClient.DefaultRequestHeaders.Add("0")
        }

        public static Jwt AuthToken = null;
        static HttpClientHandler Handler = new HttpClientHandler() {
        };

        public static HttpClient HttpClient = new HttpClient(Handler);
        public static Dictionary<Currency, decimal> CurrencyRates = new Dictionary<Currency, decimal>();

        public static string GetName(this Enum val) => Enum.GetName(val.GetType(), val);

        /// <summary>
        /// Easy HttpPost function
        /// </summary>
        /// <param name="path">Href to the server</param>
        /// <param name="form">Key-Values to send to API</param>
        /// <param name="useToken">Set to false if the request should not use the session token</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetHttpPostResponse(string path, Dictionary<string, string> form = null, bool useToken = true)
        {
            try
            {
                if (form is null) form = new Dictionary<string, string>();
                if (useToken) form.Add("token", JsonConvert.SerializeObject(AuthToken));
                using var content = new FormUrlEncodedContent(form);
                //HttpWebRequest request;

                //using var message = new HttpRequestMessage()
                //{
                //    Content = content,
                //    Method = new HttpMethod("POST"),
                //    d
                //};

                var resp = await HttpClient.PostAsync(Url+path, content);
                //Debug.WriteLine("Post response headers =>");
                foreach (var h in resp.Headers)
                {
                    Debug.WriteLine(h.Key + ": " + h.Value);
                }
                //resp.Headers.Add("");
                return resp;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"POST ERROR: {e}");

                using var content = new StringContent("Server offline");
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = content,
                };
            }
        }

        public static string GetStringResponse(string path, Dictionary<string, string> form = null)
            => GetHttpPostResponse(path, form).Result.Content.ReadAsStringAsync().Result;
        
        public static async Task<bool> VerifyMultiJwt(this Jwt jwt)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "token", JsonConvert.SerializeObject(jwt)} });
            using var resp = await HttpClient.PostAsync("https://mltapi1.azurewebsites.net/api/verify", content);
            if (resp.IsSuccessStatusCode) return true;
            else return false;
        }

        /// <summary>
        /// Returns a discount % as float
        /// </summary>
        /// <param name="type"></param>
        /// <param name="months"></param>
        /// <returns></returns>
        public static (float, float, decimal) CalculateSubDiscount(SubscriptionType type, short months)
        {
            float maxDiscount;
            var monthsMlt = 0.5f;
            int minStreak = 3;
            int streak = months - 3 <= 0 ? 0 : months - 3;
            
            float disc = 0;
            for (short i = 0; i < streak; i++)
                disc += 3f;    
            
            switch (type)
            {
                case SubscriptionType.Personal:
                    maxDiscount = 10;
                    break;
                case SubscriptionType.Pro:
                    maxDiscount = 15;
                    break;
                case SubscriptionType.Business:
                    maxDiscount = 20;
                    break;
                default: return (0,0,0);
            }

            var ndisc = (float)Math.Round(disc >= maxDiscount ? maxDiscount : disc);

            var cost = SubscriptionsMeta.SubscriptionCosts[type] * months;
            var discCost = cost - cost * ((decimal)ndisc / 100m);

            return (ndisc, ndisc / 100, discCost);
        }
    }

}
