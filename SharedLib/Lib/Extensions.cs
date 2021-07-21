using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SharedLib.Lib
{
    public static class Extensions
    {
        public static void ReplaceAll<T>(this List<T> ls, Func<T, bool> filter, T newObj)
        {
            var item = ls.FirstOrDefault(filter);
            ls.Remove(item);
            ls.Add(newObj);
        }

        public static string Print(this DateTime dt) => $"{dt.DayOfWeek.GetName()}, {dt.Day}/{dt.Month}/{dt.Year}, {dt.TimeOfDay.Hours}:{dt.TimeOfDay.Minutes}";

        public static string GetDisplayName(this MethodType method)
        {
            switch (method)
            {
                case MethodType.BecsDirect:
                    return "BECS Direct Debit";
                    break;
                case MethodType.CreditCard:
                    return "Credit Card";
                    break;
                case MethodType.SepaDirect:
                    return "SEPA Direct Debit";
                    break;
                case MethodType.Sofort:
                    return "SOFORT";
                    break;
                case MethodType.StripeInvoice:
                    return "Stripe Invoice System";
                    break;
                case MethodType.Przelewy24: return MethodType.Przelewy24.GetName(); break;

                default: return "Default Payment Method";
            }
        }
    }

    public static class PostsExtensions
    {
        public static bool VerifyContent(this JobPost post, SubscriptionType subscriptionType)
        {
            //var limits = SubscriptionsMeta.Limits[subscriptionType];

            //if (string.IsNullOrWhiteSpace(post.Title)
            //    || post.Title.Length < 10 || post.Title.Length > 200
            //    || string.IsNullOrWhiteSpace(post.Description)
            //    || post.Description.Length < 30 || post.Description.Length > 2000
            //    //|| post.Tags.Count > limits.PostTagCount
            //    //|| post.Images.Count > limits.PostImageCount
            //    ) return false;

            //post.Id = null;

            return true;
        }
        public static bool VerifyContent(this JobSearchPost post)
        {
            //if (post.UserId is null
            //    || string.IsNullOrWhiteSpace(post.Title)
            //    || post.Title.Length < 10 || post.Title.Length > 150
            //    || string.IsNullOrWhiteSpace(post.Description)
            //    || post.Description.Length < 30 || post.Description.Length > 2000
            //    || post.Tags.Count > 8) return false;

            return true;
        }
    }
}

