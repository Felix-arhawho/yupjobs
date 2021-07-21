//using MongoDB.Driver;
//using SharedLib.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using OTaff.Lib;

//namespace OTaff.Workers
//{
//    public static class InvoicesWorker
//    {
//        private static bool _working = false;
//        public static bool Start { get => _working; set { _working = value; if (value) { _ = DoWork(); } } }

//        private static async Task DoWork()
//        {
//            while (_working)
//            {
//                var cursor = Db.CustomerInvoicesCollection.FindSync(x => !x.Paid && x.Status == InvoiceStatus.WaitingForPayment, new FindOptions<CustomerInvoice, CustomerInvoice>()
//                {
//                    Sort = new SortDefinitionBuilder<CustomerInvoice>().Ascending(x => x.DateIssued),
//                    BatchSize = 100
//                });

//                while (cursor.MoveNext()) 
//                    foreach (var invoice in cursor.Current)
//                    {
//                        if (invoice.DateIssued <= DateTime.UtcNow - invoice.TimeToPay)
//                        {

//                        }
//                    }
//            }
//        }
//    }
//}
