// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AuthSamples.DynamicSchemes;

public class SimpleOptions : AuthenticationSchemeOptions
{
    public string DisplayMessage { get; set; }
}

public class SimpleAuthHandler : AuthenticationHandler<SimpleOptions>
{
    public SimpleAuthHandler(IOptionsMonitor<SimpleOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        throw new NotImplementedException();
    }
}
