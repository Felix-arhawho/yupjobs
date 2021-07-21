using MongoDB.Driver;
using Newtonsoft.Json;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OTaff.Lib.Extensions
{
    public static class Utils
    {
        public static T ToObject<T>(this string s) {
            try
            {
                return JsonConvert.DeserializeObject<T>(s);
            }
            catch
            {
                return default;
            }
        }

        public static Currency[] SingleDigits = 
            new Currency[]{
                //Currency.INR
            };

        public static long GetCents(this decimal d, Currency currency, decimal fee = 0) 
        {
            if (SingleDigits.Contains(currency)) return (long)Math.Round(d + d * fee, 0, MidpointRounding.ToPositiveInfinity);
            return (long)Math.Round(d * 100 + d * fee, 0, MidpointRounding.ToPositiveInfinity);
        }

        public static decimal GetMoney(this long l, Currency currency, decimal fee = 0) 
        {
            if (SingleDigits.Contains(currency)) return l;
            return l / 100m;
        }
        public static decimal GetMoney(this long? l, Currency currency, decimal fee = 0)
        {
            if (SingleDigits.Contains(currency)) return (decimal)l;
            return (decimal)(l / 100m);
        }

        public static string GetName(this CountryCode v) => Enum.GetName(v.GetType(), v);
        public static string ToJson<T>(this T obj, bool indented = false) => JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        public static TimeSpan GetMonthTimeSpan(this int i) => DateTime.UtcNow.AddMonths(i) - DateTime.UtcNow;
        public static TimeSpan GetMonthTimeSpan(this short i) => DateTime.UtcNow.AddMonths(i) - DateTime.UtcNow;

        /// <summary>
        /// Returns null if not found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static T First<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter) => collection.Find(filter).FirstOrDefault();
        public static Task<T> FirstAsync<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter) => collection.Find(filter).FirstOrDefaultAsync();
        public static Task<List<T>> AllAsync<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter) => collection.Find(filter).ToListAsync();
        public static List<T> All<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter) => collection.Find(filter).ToList();

        //public static void Refresh(this UserBill bill) => bill = Db.UserBillsCollection.First(x => x.Id == bill.Id);
        
        //public static T ToObj<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
    }
}
