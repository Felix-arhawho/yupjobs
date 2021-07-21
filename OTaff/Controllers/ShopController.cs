using Microsoft.AspNetCore.Mvc;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using OTaff.Lib.Money;
using ServerLib;
using SharedLib.Lib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/shop")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        [HttpPost("getprices/currency")]
        public ActionResult<string> GetPrices(Currency currency)
        {
            var prices = new Dictionary<YupShopItemType, decimal>();
            foreach (var i in YupShopItem.ShopItems)
                prices.Add(i.Key, FeesCtl.GetConvertedRate(i.Value, currency));

            return prices.ToJson();
        }

        [HttpPost("buyposts")]
        public ActionResult<string> BuyPosts([FromForm] string token, [FromForm] PostPack pack, [FromForm] string methodId, [FromForm] Currency currency)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var data = new Dictionary<string, decimal>();

            var price = FeesCtl.GetConvertedRate((int)pack, currency);
            var count = 0;
            switch (pack)
            {
                case PostPack.Pack2:
                    count = 2;
                    break;
                case PostPack.Pack5:
                    count = 5;
                    break;
                case PostPack.Pack10:
                    count = 10;
                    break;
            }


            var action = new ShopItemActionData
            {
                UserId = jwt.UserId,
                Issued = DateTime.UtcNow,
                ActionType = BillAction.ShopItem,
                ShopItem = new YupShopItem
                {
                    DateBought = DateTime.UtcNow,
                    UserId = jwt.UserId,
                    ActionData = new Dictionary<string, dynamic>
                    {
                        {"posts", count }
                    },
                    ItemType = YupShopItemType.BuyPosts,
                }
            };

            //var charge = ChargeCtl.ChargeMethod(
            //    jwt.GetUser(), 
            //    currency,
            //    Db.PaymentMethodsCollection.First(x=>x.Id==methodId&&x.UserId==jwt.UserId),
            //    price,
            //    $"Bought {pack.GetName()} for {jwt.Username}",
            //    action);

            //return charge.ToJson();
            return default;
        }

        [HttpPost("buyitem")]
        public ActionResult<string> BuyItem(
            [FromForm] string token, 
            [FromForm] YupShopItemType type, 
            [FromForm] Currency currency, 
            [FromForm] string methodId,
            [FromForm] string targetId = null,
            [FromForm] string extraMeta = null)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();

            var price = YupShopItem.ShopItems[type];

            //var charge = ChargeCtl.ChargeMethod(
            //    jwt.GetUser(), 
            //    currency, 
            //    Db.PaymentMethodsCollection.First(x => x.Id == methodId), 
            //    price, 
            //    $"Bought {type} for {jwt.Username}",
            //    new ShopItemActionData 
            //    {
            //        UserId = jwt.UserId,
            //        ActionType = BillAction.ShopItem,
            //        Issued = DateTime.UtcNow,
            //        ShopItem = new YupShopItem
            //        {
            //            UserId = jwt.UserId,
            //            ItemType = type,
            //            DateBought = DateTime.UtcNow,
            //        }
            //    });

            return default;
        }
    }

    public enum PostPack
    {
        Pack2 = 2,
        Pack5 = 5,
        Pack10 = 8,
    }
}
