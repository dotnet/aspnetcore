using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Identity.DefaultUI.WebSite.Services
{
    public class ContosoEmailSender : IEmailSender
    {
        public IList<IdentityEmail> SentEmails { get; set; } = new List<IdentityEmail>();

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            SentEmails.Add(new IdentityEmail(email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }
}
