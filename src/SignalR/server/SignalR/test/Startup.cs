// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.SignalR.Tests
{
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
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Legacy routing, runs different code path for mapping hubs
#pragma warning disable CS0618 // Type or member is obsolete
            app.UseSignalR(routes =>
            {
                routes.MapHub<AuthHub>("/authHub");
            });
#pragma warning restore CS0618 // Type or member is obsolete

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<UncreatableHub>("/uncreatable");
                endpoints.MapHub<AuthHub>("/authHubEndpoints");

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
}
