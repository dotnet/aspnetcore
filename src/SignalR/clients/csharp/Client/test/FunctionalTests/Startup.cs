// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public class Startup
{
    private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(SHA256.HashData(Guid.NewGuid().ToByteArray()));
    private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR(options => options.EnableDetailedErrors = true)
                .AddMessagePackProtocol();

        services.AddSingleton<IUserIdProvider, HeaderUserIdProvider>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            });
            options.AddPolicy(NegotiateDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(NegotiateDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.Name);
            });
        });

        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateAudience = false,
                            ValidateIssuer = false,
                            ValidateActor = false,
                            ValidateLifetime = true,
                            IssuerSigningKey = SecurityKey
                        };
                });

        // Since tests run in parallel, it's possible multiple servers will startup,
        // we use an ephemeral key provider and repository to avoid filesystem contention issues
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

        services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new EphemeralXmlRepository();
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.Use(next =>
        {
            return context =>
            {
                if (context.Request.Path.Value.EndsWith("/negotiate", StringComparison.Ordinal))
                {
                    context.Response.Cookies.Append("fromNegotiate", "a value");
                }
                return next(context);
            };
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<TestHub>("/default", o => o.AllowStatefulReconnects = true);
            endpoints.MapHub<DynamicTestHub>("/dynamic");
            endpoints.MapHub<TestHubT>("/hubT");
            endpoints.MapHub<HubWithAuthorization>("/authorizedhub");
            endpoints.MapHub<HubWithAuthorization2>("/authorizedhub2")
                  .RequireAuthorization(new AuthorizeAttribute(JwtBearerDefaults.AuthenticationScheme));
            endpoints.MapHub<HubWithAuthorization2>("/windowsauthhub")
                  .RequireAuthorization(new AuthorizeAttribute(NegotiateDefaults.AuthenticationScheme));

            endpoints.MapHub<TestHub>("/default-nowebsockets", options => options.Transports = HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents);

            endpoints.MapHub<TestHub>("/negotiateProtocolVersion12", options =>
            {
                options.MinimumProtocolVersion = 12;
            });

            endpoints.MapHub<TestHub>("/negotiateProtocolVersionNegative", options =>
            {
                options.MinimumProtocolVersion = -1;
            });

            endpoints.MapGet("/generateJwtToken/{name?}", (HttpContext context, string name) =>
            {
                return context.Response.WriteAsync(GenerateJwtToken(name ?? "testuser"));
            });

            endpoints.Map("/redirect/{*anything}", context =>
            {
                return context.Response.WriteAsync(JsonConvert.SerializeObject(new
                {
                    url = $"{context.Request.Scheme}://{context.Request.Host}/authorizedHub",
                    accessToken = GenerateJwtToken()
                }));
            });
        });
    }

    private string GenerateJwtToken(string name = "testuser")
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, name) };
        var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.Now.AddSeconds(5), signingCredentials: credentials);
        return JwtTokenHandler.WriteToken(token);
    }
}
