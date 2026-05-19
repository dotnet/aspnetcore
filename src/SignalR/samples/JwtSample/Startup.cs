// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace JwtSample;

public class Startup
{
    private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
    private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            });
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
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
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseFileServer();

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<Broadcaster>("/broadcast");
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
        var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddSeconds(30), signingCredentials: credentials);
        return JwtTokenHandler.WriteToken(token);
    }
}
