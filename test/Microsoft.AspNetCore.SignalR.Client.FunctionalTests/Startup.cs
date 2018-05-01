// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class Startup
    {
        private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
        private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            })
                .AddMessagePackProtocol();
            services.AddSingleton<IUserIdProvider, HeaderUserIdProvider>();
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
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseSignalR(routes =>
            {
                routes.MapHub<TestHub>("/default");
                routes.MapHub<DynamicTestHub>("/dynamic");
                routes.MapHub<TestHubT>("/hubT");
                routes.MapHub<HubWithAuthorization>("/authorizedhub");
                routes.MapHub<TestHub>("/default-nowebsockets", options => options.Transports = HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents);
            });

            app.Run(async (context) =>
            {
                if (context.Request.Path.StartsWithSegments("/generateJwtToken"))
                {
                    await context.Response.WriteAsync(GenerateJwtToken());
                    return;
                }
                else if (context.Request.Path.StartsWithSegments("/redirect"))
                {
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        url = $"{context.Request.Scheme}://{context.Request.Host}/authorizedHub",
                        accessToken = GenerateJwtToken()
                    }));
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
