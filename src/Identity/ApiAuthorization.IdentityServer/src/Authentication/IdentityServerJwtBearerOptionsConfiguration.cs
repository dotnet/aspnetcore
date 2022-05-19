// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

internal sealed class IdentityServerJwtBearerOptionsConfiguration : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly string _scheme;
    private readonly string _apiName;
    private readonly IIdentityServerJwtDescriptor _localApiDescriptor;

    public IdentityServerJwtBearerOptionsConfiguration(
        string scheme,
        string apiName,
        IIdentityServerJwtDescriptor localApiDescriptor)
    {
        _scheme = scheme;
        _apiName = apiName;
        _localApiDescriptor = localApiDescriptor;
    }

    public void Configure(string name, JwtBearerOptions options)
    {
        var definitions = _localApiDescriptor.GetResourceDefinitions();
        if (!definitions.ContainsKey(_apiName))
        {
            return;
        }

        if (string.Equals(name, _scheme, StringComparison.Ordinal))
        {
            options.Events = options.Events ?? new JwtBearerEvents();
            options.Events.OnMessageReceived = ResolveAuthorityAndKeysAsync;
            options.Audience = _apiName;

            var staticConfiguration = new OpenIdConnectConfiguration
            {
                Issuer = options.Authority
            };

            var manager = new StaticConfigurationManager(staticConfiguration);
            options.ConfigurationManager = manager;
            options.TokenValidationParameters.ValidIssuer = options.Authority;
            options.TokenValidationParameters.NameClaimType = "name";
            options.TokenValidationParameters.RoleClaimType = "role";
        }
    }

    internal static async Task ResolveAuthorityAndKeysAsync(MessageReceivedContext messageReceivedContext)
    {
        var options = messageReceivedContext.Options;
        if (options.TokenValidationParameters.ValidIssuer == null || options.TokenValidationParameters.IssuerSigningKey == null)
        {
            var store = messageReceivedContext.HttpContext.RequestServices.GetRequiredService<ISigningCredentialStore>();
            var credential = await store.GetSigningCredentialsAsync();
#pragma warning disable 0618
            options.Authority = options.Authority ?? messageReceivedContext.HttpContext.GetIdentityServerIssuerUri();
#pragma warning restore 0618
            options.TokenValidationParameters.IssuerSigningKey = credential.Key;
            options.TokenValidationParameters.ValidIssuer = options.Authority;
        }
    }

    public void Configure(JwtBearerOptions options)
    {
    }
}
