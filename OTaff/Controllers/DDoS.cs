using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    public static class DDoS
    {
        //public static SynchronizedCollection<>

        public static ConcurrentDictionary<HostString, Tuple<LogDateTime, RequestList>> RequestsCollection = new ConcurrentDictionary<HostString, Tuple<LogDateTime, RequestList>>();

        static string ApiKey = "apikey";

        //public static bool VerifyApiKey()
        //{
        //    return ApiKey == 
        //}

        public static long CurrentRequests = 0;
        public static long MaxRequests = 10000;

        public static object LockObject = new object();

        public static Task<bool> HandleRequest(ref HttpContext context, bool reqKey = false)
        {
            Throttle();

            //if (reqKey && context.Request.Form["apikey"] != ApiKey) return false;

            if (RequestsCollection.TryGetValue(context.Request.Host, out var tuple))
            {
                if (tuple.Item2._requests.Count(x => x.Time > DateTime.UtcNow - TimeSpan.FromMinutes(1)) > 100)
                    return Task.FromResult(false);
            }

            var log = new ClientRequestLog()
            {
                Request = context.Request,
                Path = context.Request.PathBase,
                Time = DateTime.UtcNow
            };

            if (RequestsCollection.ContainsKey(context.Request.Host)) {
                RequestsCollection[context.Request.Host].Item2.Add(log);
                RequestsCollection[context.Request.Host].Item1.SetNow();
            }
            else RequestsCollection.TryAdd(context.Request.Host, new Tuple<LogDateTime, RequestList>(LogDateTime.Now, new RequestList(log)));
            var ctx = context;
            //if (RequestsCollection[ctx.Request.Host].Item2._requests.Count(g=>g.Request.Path.ToUriComponent().Contains("auth/register")) > 3)
            //{
            //    return Task.FromResult(false);
            //}

            //RequestsCollection[context.Request.Host]

            Interlocked.Decrement(ref CurrentRequests);
            return Task.FromResult(true);
        }

        public static void Throttle()
        {
            lock (LockObject)
            {
                while (CurrentRequests >= MaxRequests) Task.Delay(5).Wait();
                Interlocked.Increment(ref CurrentRequests);
            }
        }
    }

    public class LogDateTime
    {
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        public void SetNow() => DateTime = DateTime.UtcNow;
        public void SetTime(DateTime time) => DateTime = time;

        public static LogDateTime Now { get => new LogDateTime() { DateTime = DateTime.UtcNow}; }
    }

    public class ClientRequestLog
    {
        public HttpRequest Request { get; set; }
        public PathString Path { get; set; }
        public DateTime Time { get; set; }
    }

    public class RequestList
    {
        public RequestList()
        {

        }
        public RequestList(ClientRequestLog log)
        {
            _requests.Add(log);
        }

        public List<ClientRequestLog> _requests = new List<ClientRequestLog>();
        private object Lock = new object();

        public void Add(ClientRequestLog log)
        {
            lock (Lock) _requests.Add(log);
        }

        public void Add(IEnumerable<ClientRequestLog> items)
        {
            lock (Lock) _requests.AddRange(items);
        }

        public void Remove(Predicate<ClientRequestLog> exp)
        {
            lock (Lock) _requests.RemoveAll(exp);
        }


    }
}
