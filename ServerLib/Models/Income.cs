//using System;
//using System.Collections.Generic;
//using System.Text;
//using Google.Cloud.Firestore;
//using Google.Cloud.Firestore.V1;

//namespace ServerLib.Models
//{
//    public static class FSDb
//    {
//    }

//    public class Income
//    {
//        [FirestoreDocumentId]
//        public string Id { get; set; }
//        [FirestoreProperty] public double Amount { get; set; }
//        [FirestoreProperty] public string Currency { get; set; }
//        [FirestoreProperty] public DateTime Date { get; set; }
//        [FirestoreProperty] public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
//    }

//}
