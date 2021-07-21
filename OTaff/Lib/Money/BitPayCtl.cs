using BitPaySDK;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Money
{
    public static class BitPayCtl
    {
        static BitPay bitpay = new BitPay(
            Env.Test,
            "Config/bitpay_private_test.key",
            new Env.Tokens()
            {
                Merchant = "7iHstvdujwXRHCJcKfygwuN9bUx3SoRStwcmKB5uuDiW",
                //Payout = "9pJ7fzW1GGeucVMcDrs7HDQfj32aNATCDnyY6YAaHUNo"
            }
        );

        public static async Task<BitPaySDK.Models.Invoice.Invoice> CreateInvoice(User user, decimal amount, Currency currency)
        {
            var invoice = new BitPaySDK.Models.Invoice.Invoice((double)amount, Enum.GetName<Currency>(currency));
            invoice.BuyerProvidedEmail = user.Email;
            invoice = await bitpay.CreateInvoice(invoice);
            return invoice;
        }
    }
}
