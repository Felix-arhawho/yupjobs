using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using ServerLib;
using Stripe;
using ServerLib.Models;

namespace OtaffSupport.Workers
{
    

    public static class DisputesWorker
    {
        static object StripeLock = new object();
        public static DisputeService Service = new DisputeService();

        public static async Task HandleDisputes()
        {
            var disputes = Service.List();

            while (true)
            {
                var tls = new Task[0];
                foreach (var d in disputes)
                {
                    var t = Task.Run(delegate 
                    {
                        if (Db.ChargeDisputes.CountDocuments(x => x.DisputeId == d.Id) > 0) 
                        {
                            var upd = new UpdateDefinitionBuilder<ChargeDispute>().Set(x => x.DisputeObj, d);
                            //Db.ChargeDisputes
                        }
                        else
                        {

                        }

                    });
                    tls.Append(t);
                }
            }
        }

    }


}
