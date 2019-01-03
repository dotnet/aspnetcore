using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthSamples.Options.MultiTenant
{
    public class SimpleOptions : AuthenticationSchemeOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class SimpleAuthHandler : AuthenticationHandler<SimpleOptions>
    {
        public SimpleAuthHandler(IOptionsMonitor<SimpleOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }
    }
}
