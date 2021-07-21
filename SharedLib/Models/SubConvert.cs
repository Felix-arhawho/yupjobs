using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Models
{
    public static class SubConvert
    {
        public static float PersoToPro = 0.4f;
        public static float PersoToBiz = 0.25f;
        public static float ProToPerso = 1.4f;
        public static float ProToBiz = 0.4f;
        public static float BizToPerso = 1.8f;
        public static float BizToPro = 1.4f;

        public static TimeSpan ConvertSub(TimeSpan time, SubscriptionType baseType, SubscriptionType targetType)
        {
            var newTime = TimeSpan.Zero;
            if (baseType == targetType)
                return time;
            
            if (baseType is SubscriptionType.Personal && targetType is SubscriptionType.Pro)
            {
                newTime = time * SubConvert.PersoToPro;
            }
            else if (baseType is SubscriptionType.Personal && targetType is SubscriptionType.Business)
            {
                newTime = time * SubConvert.PersoToBiz;
            }
            else if (baseType is SubscriptionType.Pro && targetType is SubscriptionType.Personal)
            {
                newTime = time * SubConvert.ProToPerso;
            }
            else if (baseType is SubscriptionType.Pro && targetType is SubscriptionType.Business)
            {
                newTime = time * SubConvert.ProToBiz;
            }
            else if (baseType is SubscriptionType.Business && targetType is SubscriptionType.Personal)
            {
                newTime = time * SubConvert.BizToPerso;
            }
            else if (baseType is SubscriptionType.Business && targetType is SubscriptionType.Pro)
            {
                newTime = time * SubConvert.BizToPro;
            }

            return newTime;
        }
    }

    
}
