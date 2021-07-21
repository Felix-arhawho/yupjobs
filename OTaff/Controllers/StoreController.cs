//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using OTaff.Lib.Extensions;
//using OTaff.Lib.Money;
//using ServerLib;
//using SharedLib.Lib;
//using SharedLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace OTaff.Controllers
//{
//    [Route("api/search")]
//    [ApiController]
//    public class StoreController : ControllerBase
//    {
//        [HttpPost("search/{search}")]
//        public ActionResult<string> Search()
//        {

//        }

//        [HttpPost("newitem")]
//        public ActionResult<string> NewItem()
//        {

//        }

//        [HttpPost("removeitem")]
//        public ActionResult<string> RemoveItem()
//        {

//        }

//        [HttpPost("buy")]
//        public ActionResult<string> BuyItem([FromForm] string token, [FromForm] string itemId, [FromForm] string methodId)
//        {
//            var jwt = token.ToObject<Jwt>();
//            if (!jwt.Verify()) return Unauthorized();

//            var item = Db.StoreItems.First(x => x.Id == itemId);
//            var method = Db.PaymentMethodsCollection.First(x => x.Id == methodId);

//            FeesCtl.

//            var charge = ChargeCtl.ChargeMethod(
//                jwt.GetUser(),
//                item.Currency,

//                );
//        }
//    }
//}
