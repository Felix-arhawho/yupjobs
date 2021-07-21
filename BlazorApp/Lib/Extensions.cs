using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Tewr.Blazor.FileReader;
using SharedLib.Models;
using SharedLib.Lib;

namespace BlazorApp.Lib
{
    public static class Extensions
    {
        public static T FromJson<T>(this string s) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s);
        public static string ToJson<T>(this T obj) => Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        
        public static async Task<HttpResponseMessage> PostImage(this IFileReference file, MediaType type = MediaType.General)
        {
            var form = await file.GenerateImageForm();
            var ret = await Ez.HttpClient.PostAsync(Ez.Url+"media/uploadimage", form);
            form.Dispose();
            return ret;
        }

        public static async Task<MultipartFormDataContent> GenerateImageForm(this IFileReference file, MediaType type = MediaType.General)
        {
            try
            {
                var content = new MultipartFormDataContent();
                /*using*/ var stream = await file.OpenReadAsync();
                var info = await file.ReadFileInfoAsync();
                
                var fcontent = new StreamContent(stream);
                /*using*/ var token = new StringContent(Session.Token.ToJson());
                /*using*/ var stype = new StringContent($"{(int)type}");

                content.Add(fcontent, "image", info.Name);
                content.Add(token, "token");
                content.Add(stype, "type");

                //stream.Dispose();

                return content;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] ReadToEnd(this System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}
