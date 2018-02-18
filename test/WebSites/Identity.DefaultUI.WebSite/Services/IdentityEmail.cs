using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.DefaultUI.WebSite.Services
{
    public class IdentityEmail
    {
        public IdentityEmail(string to, string subject, string body)
        {
            To = to;
            Subject = subject;
            Body = body;
        }

        public string To { get; }
        public string Subject { get; }
        public string Body { get; }
    }
}
