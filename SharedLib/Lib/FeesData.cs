using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Lib
{
    public static class FeesCtl
    {
        /// <summary>
        /// EUR base
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public static decimal GetConvertedRate(decimal amount, Currency currency, decimal fee = 0.02m, bool roundToHigher = false)
        {
            if (currency is Currency.EUR) return amount;
            //if (currency is Currency.INR) roundToHigher = true;

            amount = amount + (amount * fee);
            var namt = amount * Ez.CurrencyRates[currency];

            if (roundToHigher)
                return Math.Round(namt, 0);
            else return Math.Round(namt, 2);
        }

        public static decimal ConvertToEur(Currency currency, decimal amount)
        {
            if (currency is Currency.EUR) return amount;

            var curVal = Ez.CurrencyRates[currency]; // the 1.85
            return Math.Round(amount / curVal, 2);
        }

        /// <summary>
        /// 0 is fee, 1 is total with fee
        /// </summary>
        /// <param name="total"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static decimal[] WalletRechargeFee(decimal total, Currency currency = Currency.EUR)
        {
            var minEurFee = 3m;
            var rechFee1 = 0.06m;
            var rechFee2 = 0.045m;
            var rechFee3 = 0.035m;

            decimal fee = GetConvertedRate(minEurFee, currency, 0);
            var eurTotal = ConvertToEur(currency, total);

            if (eurTotal < 50)
            {
                fee += (total*rechFee1);
            }else if (eurTotal < 150)
            {
                fee += (total * rechFee2);
            }else if (eurTotal >= 150)
            {
                fee += (total * rechFee3);
            }
            return new[] { fee, fee+total, total };
        }

        public static decimal[] ChargeFee(decimal amount, float fee = 0.15f)
        {
            var mlt = (int)(fee * 100 + 100);
            var calc = amount / mlt;
            var taxAmount = (decimal)fee * 100m;
            return new[] { amount, taxAmount };
        }
    }
}
