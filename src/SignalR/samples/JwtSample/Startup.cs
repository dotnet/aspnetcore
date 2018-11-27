// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JwtSample
{
    public class Startup
    {
        private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseFileServer();
            app.UseSignalR(options => options.MapHub<Broadcaster>("/broadcast"));

            var routeBuilder = new RouteBuilder(app);
            routeBuilder.MapGet("generatetoken", c => c.Response.WriteAsync(GenerateToken(c)));
            app.UseRouter(routeBuilder.Build());
        }

        private string GenerateToken(HttpContext httpContext)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, httpContext.Request.Query["user"]) };
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.UtcNow.AddSeconds(30), signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }
    }
}
