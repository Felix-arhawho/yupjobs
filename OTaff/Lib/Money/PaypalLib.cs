using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PayPal.Util;
using PayPal.Log;
using PayPal.Api;
using PayPal.Api.OpenIdConnect;
using PayPalCheckoutSdk.Core;
using SharedLib.Models;
using PayPalCheckoutSdk.Orders;

namespace OTaff.Lib.Money
{
    public static class PaypalController
    {
        static PaypalController()
        {
            // Creating a sandbox environment
            PayPalEnvironment environment = new SandboxEnvironment(
                "AVdo6Swi5zX0ogmi0C4VeGLTyy_c6MRsWeCNakZNP2Xnii89EU63MFmnCVQQgUSHFtldmVOv6EGFQHsU",
                "EG2CkCYwMnPxA4L6deaTsu9B_TBu_5YLX_bkewi4sAu8UczbBn87lyn1T-3V51x5J95hJr4DSO78vimj");
            Client = new PayPalHttpClient(environment);
        }

        public static PayPalHttpClient Client = null;

        //public static Dictionary<string, dynamic> CreateOrder(
        //    User user,
        //    decimal amount,
        //    SharedLib.Models.Currency currency,
        //    string description,
        //    BillActionData action = null,
        //    UserBill bill = null
        //    )
        //{
            //var order = new OrderRequest()
            //{
            //    CheckoutPaymentIntent = "CAPTURE",
            //    PurchaseUnits = new List<PurchaseUnitRequest>()
            //    {
            //        new PurchaseUnitRequest()
            //        {
            //            AmountWithBreakdown = new AmountWithBreakdown()
            //            {
            //                CurrencyCode = Enum.GetName(currency)
            //            }
            //        }
            //    },
            //    ApplicationContext = new ApplicationContext()
            //    {
            //        BrandName = "YupJobs",
            //        ReturnUrl = $"{SharedLib.Lib.Ez.ClientUrl}success/pay/{bill.Id}",
            //        CancelUrl = $"{SharedLib.Lib.Ez.ClientUrl}success/pay/cancelled",                    
            //    },
            //};


            //var request = new OrdersCreateRequest();
            //request.RequestBody(order);


            //request.Prefer("return=representation");
            //request.RequestBody(order);
            //response = await client().Execute(request);
            //var statusCode = response.StatusCode;
            //Order result = response.Result<Order>();
            //Console.WriteLine("Status: {0}", result.Status);
            //Console.WriteLine("Order Id: {0}", result.Id);
            //Console.WriteLine("Intent: {0}", result.Intent);
            //Console.WriteLine("Links:");
            //foreach (LinkDescription link in result.Links)
            //{
            //    Console.WriteLine("\t{0}: {1}\tCall Type: {2}", link.Rel, link.Href, link.Method);
            //}
            //return response;
        //}
    }
}
