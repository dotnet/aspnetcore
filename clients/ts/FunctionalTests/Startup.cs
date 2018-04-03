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
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;

namespace FunctionalTests
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
            })
                .AddJsonProtocol(options =>
                {
                    // we are running the same tests with JSON and MsgPack protocols and having
                    // consistent casing makes it cleaner to verify results
                    options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
                })
                .AddMessagePackProtocol();

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
                            var signalRTokenHeader = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(signalRTokenHeader) &&
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();
            app.UseConnections(routes =>
            {
                routes.MapConnectionHandler<EchoConnectionHandler>("/echo");
            });

            app.UseSignalR(routes =>
            {
                routes.MapHub<TestHub>("/testhub");
                routes.MapHub<TestHub>("/testhub-nowebsockets", options => options.Transports = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling);
                routes.MapHub<UncreatableHub>("/uncreatable");
                routes.MapHub<HubWithAuthorization>("/authorizedhub");
            });

            app.Use(next => async (context) =>
            {
                if (context.Request.Path.StartsWithSegments("/generateJwtToken"))
                {
                    await context.Response.WriteAsync(GenerateJwtToken());
                    return;
                }
            });
        }

        private string GenerateJwtToken()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "testuser") };
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("SignalRTestServer", "SignalRTests", claims, expires: DateTime.Now.AddSeconds(5), signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }
    }
}
