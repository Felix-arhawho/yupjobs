
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace OTaff.Lib
{
    [Serializable]
    public class EmailRequest
    {
        [NotNull] public string subject { get; set; }
        [NotNull] public string api_key = MailController.GoApiKey;
        [NotNull] public string html_body { get; set; }

        [NotNull] public string sender { get; set; }
        [NotNull] public List<string> to { get; set; }

    }
    public static class MailController
    {
        static MailController()
        {
            //Client.EnableSsl = true;
            //Client.Host = "smtp.gmail.com";
            //Client.UseDefaultCredentials = true;
            //Client.Credentials = new NetworkCredential("service@multia2z.com", "26paris1373");
        }

        public static string GoApiKey = "api-1962B032A1A511EAB81DF23C91C88F4E";
        //public static ;
        public static HttpClient Http = new HttpClient();

        //public static bool SendSmtp(MailMessage mail)
        //{
        //    //Client.Send(mail);
        //    //return true; 
        //}

        //public static Task SendSmtpOtp(string code, string userId, string recipient)
        //{
        //    Console.WriteLine($"Sending email to => {recipient}");
        //    SendOtp(code, userId, recipient);
        //    return Task.CompletedTask;
        //    SmtpClient client = new SmtpClient
        //    {
        //        EnableSsl = true,
        //        Host = "smtp.gmail.com",
        //        Port = 465,
        //        UseDefaultCredentials = false,
        //        Credentials = new NetworkCredential("service@multia2z.com", "")
        //    };
        //    var otp = new Otp
        //    {
        //        Code = code,
        //        Type = OtpType.MailVerif,
        //        UserId = userId
        //    };
        //    Db.OtpCollection.InsertOneAsync(otp);
        //    Console.WriteLine("Saved otp");
        //    client.SendMailAsync(new MailMessage
        //    {
        //        IsBodyHtml = true,
        //        Body = $"<h3>Your OTP Code is <h2>{code}</h2></h3>",
        //        From = new MailAddress("noreply@multia2z.com", "Tuyau.net"),
        //        Subject = "Confirmation code",
        //        //Sender = new MailAddress("noreply@multia2z.com", "Tuyau.net"),
        //        To = { new MailAddress(recipient) },
        //        Priority = MailPriority.High
        //    }).Wait();
        //    Console.WriteLine("Send mail to => " + recipient);
        //    client.Dispose();
        //    return Task.CompletedTask;
        //}

        public static bool SendOtp(string code, string userId, string recipient)
        {
            Task.Run(() =>
            {
                var request = new EmailRequest()
                {
                    html_body = $"<h3>Your otp is <h2>{code}</h2></h3>",
                    sender = "TuyauNet <support@tuyau.net>",
                    subject = "Confirmation code",
                    to = new List<string>() { $"<{recipient}>" },
                    api_key = GoApiKey,
                };
                //var form = new StringContent(JsonConvert.SerializeObject(request));
                Http.PostAsync("https://api.smtp2go.com/v3/email/send",
                    new StringContent(JsonConvert.SerializeObject(request)));
            });

            return true;
        }


        public static Task SendMail(string htmlContent, string subject, string recipient)
        {
            var request = new EmailRequest()
            {
                html_body = $"<div>{htmlContent}</div>",
                sender = "YupJobs <support@tuyau.net>",
                subject = subject,
                to = new List<string>() { $"<{recipient}>" },
                api_key = GoApiKey,
            };

            Http.PostAsync(
                "https://api.smtp2go.com/v3/email/send",
                new StringContent(JsonConvert.SerializeObject(request)));

            return Task.CompletedTask;
        }

        public static Task<bool> SendSmtp(MailMessage mail)
        {
            try
            {
                using var client = new SmtpClient() {
                    Host = "mail.yupjobs.net",
                    Port = 25,
                    //EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("noreply", "thisanicepassword"),
                    //Credentials = 
                };
                client.Send(mail);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Task.FromResult(false);
            }
        }
    }
}
