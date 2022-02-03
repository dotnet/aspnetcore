// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class Startup
{
    private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
    private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConnections();
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    LifetimeValidator = (before, expires, token, parameters) => expires > DateTime.UtcNow,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateActor = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = SecurityKey
                };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Accept"] == "text/event-stream"))
                    {
                        context.Token = context.Request.Query["access_token"];
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        services.AddSingleton<IAuthorizationHandler, TestAuthHandler>();

        // Since tests run in parallel, it's possible multiple servers will startup and read files being written by another test
        // Use a unique directory per server to avoid this collision
        services.AddDataProtection()
            .PersistKeysToFileSystem(Directory.CreateDirectory(Path.GetRandomFileName()));
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<UncreatableHub>("/uncreatable");
            endpoints.MapHub<AuthHub>("/authHub");

            endpoints.MapConnectionHandler<EchoConnectionHandler>("/echo");
            endpoints.MapConnectionHandler<WriteThenCloseConnectionHandler>("/echoAndClose");
            endpoints.MapConnectionHandler<HttpHeaderConnectionHandler>("/httpheader");
            endpoints.MapConnectionHandler<AuthConnectionHandler>("/auth");

            endpoints.MapGet("/generatetoken", context =>
            {
                return context.Response.WriteAsync(GenerateToken(context));
            });
        });
    }

    private string GenerateToken(HttpContext httpContext)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, httpContext.Request.Query["user"]) };
        var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddMinutes(1), signingCredentials: credentials);
        return JwtTokenHandler.WriteToken(token);
    }
}
