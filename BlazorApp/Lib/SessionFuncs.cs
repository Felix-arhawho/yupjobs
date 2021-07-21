using Newtonsoft.Json;
using SharedLib.Lib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp.Lib
{
    public static partial class Session
    {
        public static async Task RefreshPaymentMethods()
        {
            using var resp = await Ez.GetHttpPostResponse("money/findmethods");
            if (resp.IsSuccessStatusCode)
                UserPaymentMethods = JsonConvert.DeserializeObject<List<UserPaymentMethod>>(await resp.Content.ReadAsStringAsync());   
        }

        public static async Task RefreshMessages()
        {
            using var resp = await Ez.GetHttpPostResponse("chat/getmessages");
            if (resp.IsSuccessStatusCode)
                ChatMessages = (await resp.Content.ReadAsStringAsync()).FromJson<List<ChatMessage>>();
        }

        public static async Task<bool> WaitForLogin(short seconds = 5) 
        {
            var cnt = 0;
            while (!Session.GotInfo)
            {
                await Task.Delay(1000);
                cnt++;
                if (cnt > 5)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
