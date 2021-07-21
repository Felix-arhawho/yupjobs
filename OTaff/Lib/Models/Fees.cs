using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OTaff.Lib.Extensions;
using ServerLib.Models;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Models
{
    public static class Extensions
    {
        //public static async Task RegisterFee(
        //    this FeeData fee,
        //    Currency currency, 
        //    decimal amount, 
        //    string metaId, 
        //    FeeType type = FeeType.Transaction,
        //    IClientSessionHandle s = null)
        //{
        //    if (s is null) 
        //        Db.FeesCollection.InsertOne(new FeeData()
        //                    {
        //                        Currency = currency,
        //                        MetaId = metaId,
        //                        Type = type,
        //                        Units = amount.GetCents(currency)
        //                    });
        //    else 
        //        Db.FeesCollection.InsertOne(s, new FeeData()
        //        {
        //            Currency = currency,
        //            MetaId = metaId,
        //            Type = type,
        //            Units = amount.GetCents(currency)
        //        });
        //}
    }

    public enum FeeType
    {
        Payment = 0,
        Transaction = 1
    }
}
