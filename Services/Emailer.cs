using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJobTracker.Services
{
    public class Emailer
    {
        private string from = "jobtrackersimple2@gmail.com";
        private string password = "qfgirbyeydskhnmv";
        private string host = "smtp.gmail.com";
        private int port = 587;

        public Emailer() { }

        public async Task SendEmailAsync(string to, string email) {
            MailMessage message = new MailMessage(from, to);
            message.Subject = "Job Tracker noticed a change on a job page";
            message.Body = email;

            SmtpClient client = new SmtpClient();

            try
            {
                client.Host = host;
                client.Port = port;
                client.Credentials = new NetworkCredential(from, password);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.Write("ERROR: " + ex.ToString());
            }
        }
    }
}
