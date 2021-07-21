using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MailRelay.Controllers
{
    [Route("api/mail")]
    [ApiController]
    public class MailController : ControllerBase
    {
        [HttpPost("send")]
        public ActionResult<string> SendMail([FromForm] string to, [FromForm] string subject, [FromForm] string body, [FromForm] string token = "temptoken123")
        {
            try
            {
                body = new string(body.Prepend('\'').ToArray());
                body = new string(body.Append('\'').ToArray());

                subject = new string(subject.Prepend('\'').ToArray());
                subject = new string(subject.Append('\'').ToArray());

                var cmd = $"echo {body} | mail -r noreply@yupjobs.net -s {subject} {to}";
                

                var args = $"-c \"{cmd}\"";

                //Console.WriteLine(args);

                var info = new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = args,
                    UseShellExecute = true,
                    CreateNoWindow = true,

                };
                using var p = new Process() { StartInfo = info };
                p.Start();

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
