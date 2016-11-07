using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public class SignalROptionsSetup : IConfigureOptions<SignalROptions>
    {
        public void Configure(SignalROptions options)
        {
            options.RegisterInvocationAdapter<JsonNetInvocationAdapter>("json");
        }
    }
}