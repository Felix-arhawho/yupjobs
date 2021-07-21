using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class FeeCalculator
    {
        public static decimal WalletRechargeFee(decimal amount, decimal fee = 0.03m)
        {
            var feeAmount = amount * fee;
            feeAmount = feeAmount < 2.50m ? 2.50m : feeAmount;
            return feeAmount;
        }
    }
}
