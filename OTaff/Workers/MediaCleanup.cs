using BunnyCDN.Net.Storage.Models;
using MongoDB.Driver;
using OTaff.Controllers;
using OTaff.Lib;
using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Workers
{
    public static class MediaCleanup
    {
        public static async Task Cleanup()
        {
            
            while (true)
            {
                try
                {
                    Console.WriteLine("[WORKER][MEDIA] Media cleanup worker has started");
                    CleanupProfiles();
                    CleanupChatImages();
                    CleanupJobPosts();
                    CleanupJobSearchPosts();
                    CleanupGarbageImages();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                await Task.Delay(1000 * 60 * 60 * 6);
            }
        }

        private static async void CleanupProfiles()
        {

        }

        private static async void CleanupJobPosts()
        {
            //var cursor = Db.UserMediaCollection.FindSync(x => x.Type == SharedLib.Models.MediaType.PostImage, new FindOptions<Lib.Models.UserMedia, Lib.Models.UserMedia> 
            //{
            //    BatchSize = 200,
            //    Sort = new SortDefinitionBuilder<Lib.Models.UserMedia>().Ascending(x => x.DateAdded)
            //});
            //while (cursor.MoveNext()){
            //    foreach (var meta in cursor.Current){
            //        if (Db.JobPostsCollection.CountDocuments(x => x.Images.Contains(meta.FullUrl)) is 0)
            //        {

            //        }
            //    }
            //}


        }

        private static async void CleanupJobSearchPosts()
        {

        }

        private static async void CleanupChatImages()
        {

        }

        /// <summary>
        /// Cleanup of all unlisted images
        /// </summary>
        private static void CleanupGarbageImages()
        {
            var dres = Db.UserMediaCollection.DeleteMany(x => x.ExpiresOn < DateTime.UtcNow);

            Console.WriteLine($"[WORKER][MEDIA] {dres.DeletedCount} media items have been deleted");

            var objects = MediaController.StorageService.GetStorageObjectsAsync("/tuyaunet/").Result;
            Console.WriteLine($"[WORKER][MEDIA] {objects.Count} objects are present in CDN");
            var metas = Db.UserMediaCollection.FindSync(x => true).ToList();
            Console.WriteLine($"[WORKER][MEDIA] {metas.Count} media items are present in DB");

            Parallel.ForEach(metas, (meta) =>
            {
                var obj = objects.FirstOrDefault(x => x.ObjectName == meta.Name);
                if (obj is null) Db.UserMediaCollection.DeleteOneAsync(x => x.Id == meta.Id);
            });
            Parallel.ForEach(objects, (obj) => 
            {
                var meta = metas.FirstOrDefault(x => x.Name == obj.ObjectName);
                if (meta is null) MediaController.StorageService.DeleteObjectAsync(obj.FullPath);
            });

            objects.Clear();
            metas.Clear();
        }
    }
}
