using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using Newtonsoft.Json;
using SharedLib.Models;
using OTaff.Lib.Extensions;

using BunnyCDN.Net.Storage;
using BunnyCDN.Net.Storage.Models;
using OTaff.Lib;
using ImageProcessor;
using System.IO;
//using ImageProcessor.Processors;
//using ImageProcessor.Plugins.WebP.Imaging.Formats;
using OTaff.Lib.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Cors;
using Aspose.Imaging.FileFormats.Webp;
using ServerLib;
using ServerLib.Models;
//using System.Drawing.;

namespace OTaff.Controllers
{
    [Route("api/media")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        public static readonly BunnyCDNStorage StorageService = new BunnyCDNStorage("tuyaunet", "418ed410-e50f-4ecc-b2beacdca2da-0ec3-4a87");

        [HttpPost("uploadb64image")]
        [DisableRequestSizeLimit]
        public ActionResult<string> UploadB64Image(
            [FromForm] string token,
            [FromForm] string b64,
            [FromForm] int days = 30,
            [FromForm] MediaType type = MediaType.AccountImage)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return Unauthorized();
            //Aspose.Imaging.FileFormats.Webp.
            using var factory = new ImageFactory();
            using var uploadStream = new MemoryStream();

            var bytes = Convert.FromBase64String(b64);
            if (bytes.Length > 1000 * 1000 * 1000 * 2) return BadRequest("File cannot exceed 2MB");

            factory.Load(bytes);
            bytes = null;
            var name = $"{DateTime.UtcNow.Ticks}-{Guid.NewGuid()}.webp";

            //factory.Format()
            //        .Quality(80)
            //        .Save(uploadStream);

            //var img = new WebPImage(uploadStream);

            StorageService.UploadAsync(uploadStream, $"/tuyaunet/{name}").Wait();


            var media = new UserMedia()
            {
                DateAdded = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow + TimeSpan.FromDays(1),
                FullUrl = $"https://tuyaunet.b-cdn.net/{name}",
                Name = name,
                UserId = jwt.UserId,
                HttpFormat = "image/webp"
            };
            Db.UserMediaCollection.InsertOne(media);
            return media.FullUrl;
        }

        //[EnableCors("AllowAll")]
        [HttpPost("uploadimage")]
        public ActionResult<string> UploadImage(
            [FromForm] string token,
            [FromForm] IFormFile image,
            [FromForm] int days = 30,
            [FromForm] MediaType type = MediaType.AccountImage)
        {
            try
            {
                var jwt = token.ToObject<Jwt>();
                if (!jwt.Verify()) return Unauthorized();

                if (image is null) return BadRequest("Image is null");
                //if (image.Length > 0) return View();

                //if (image.Length > 1000 * 1000 * 1000 * 2) return BadRequest("File cannot exceed 2MB");

                string[] allowedImageTypes = new string[] { "image/jpeg", "image/png", "image/webp" };

                //using var factory = new ImageFactory();
                //using var imageStream = image.OpenReadStream();
                //using var uploadStream = new MemoryStream(); /*image.OpenReadStream();*/
                //factory
                //    .Load(imageStream)
                //    .Format(new WebPFormat())
                //    .Quality(80)
                //    .Save(uploadStream);

                //var 
                //var img = Aspose.Imaging.Image.Load(image.OpenReadStream());
                //img.Save(uploadStream);

                var name = $"{DateTime.UtcNow.Ticks}-{Guid.NewGuid()}";
                StorageService.UploadAsync(image.OpenReadStream(), $"/tuyaunet/{name}").Wait();

                var media = new UserMedia() {
                    DateAdded = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow + TimeSpan.FromDays(1),
                    FullUrl = $"https://tuyaunet.b-cdn.net/{name}",
                    Name = name,
                    UserId = jwt.UserId,
                    HttpFormat = "image/webp"
                };
                Db.UserMediaCollection.InsertOne(media);
                return media.FullUrl;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest("Unknown error, try again later");
            }

        }

        [HttpGet("image/{id}/{tokenId}/{issuedSecond}")]
        public ActionResult<string> GetImage(string id, string tokenId, string issuedSecond)
        {
            //DDOS protect
            try
            {
                var image = StorageService.DownloadObjectAsStreamAsync($"/tuyaunet/{id}.webp").Result;
                return File(image, "image/webp");
            }
            catch
            {
                return NotFound("File not found");
            }
        }

        [HttpPost("delete")]
        public void Delete([FromForm] string token, [FromForm] string url)
        {
            var jwt = token.ToObject<Jwt>();
            if (!jwt.Verify()) return;

            var media = Db.UserMediaCollection.First(x => x.FullUrl == url && x.UserId == jwt.UserId);
            if (media is null) return;

            _ = StorageService.DeleteObjectAsync($"/tuyaunet/{media.Name}.webp");
            _ = Db.UserMediaCollection.DeleteOneAsync(x => x.FullUrl == url);
        }

        [HttpPost("setexpiry")]
        public void SetExpiry([FromForm] string id, [FromForm] DateTime? expiryDate = null)
        {
            if (expiryDate is null)
                expiryDate = DateTime.UtcNow.AddDays(31);

            Db.UserMediaCollection.UpdateOne(x => x.FullUrl == id, new UpdateDefinitionBuilder<UserMedia>().Set(x=>x.ExpiresOn, expiryDate));
        }
    }
}
