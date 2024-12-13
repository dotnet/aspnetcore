// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FunctionalTests;

public class Startup
{
    private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(SHA256.HashData(Guid.NewGuid().ToByteArray()));
    private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();

    private int _numRedirects;

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
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        })
        .AddMessagePackProtocol();

        services.AddCors();

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
                        var endpoint = context.HttpContext.Features.Get<IEndpointFeature>()?.Endpoint;
                        if (endpoint != null && endpoint.Metadata.GetMetadata<HubMetadata>() != null)
                        {
                            var request = context.HttpContext.Request;
                            string token = request.Headers["Authorization"];

                            if (!string.IsNullOrEmpty(token))
                            {
                                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    token = token.Substring("Bearer ".Length).Trim();
                                }
                            }
                            else
                            {
                                token = context.Request.Query["access_token"];
                            }

                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
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

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseFileServer();

        // Custom CORS to allow any origin + credentials (which isn't allowed by the CORS spec)
        // This is for testing purposes only (karma hosts the client on its own server), never do this in production
        app.Use((context, next) =>
        {
            var originHeader = context.Request.Headers.Origin;
            if (!StringValues.IsNullOrEmpty(originHeader))
            {
                logger.LogInformation("Setting CORS headers.");
                context.Response.Headers.AccessControlAllowOrigin = originHeader;
                context.Response.Headers.AccessControlAllowCredentials = "true";

                var requestMethod = context.Request.Headers.AccessControlRequestMethod;
                if (!StringValues.IsNullOrEmpty(requestMethod))
                {
                    context.Response.Headers.AccessControlAllowMethods = requestMethod;
                }

                var requestHeaders = context.Request.Headers.AccessControlRequestHeaders;
                if (!StringValues.IsNullOrEmpty(requestHeaders))
                {
                    context.Response.Headers.AccessControlAllowHeaders = requestHeaders;
                }
            }

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                logger.LogInformation("Setting '204' CORS response.");
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }

            return next.Invoke(context);
        });

        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/echoredirect"))
            {
                var url = context.Request.Path.ToString();
                url = url.Replace("echoredirect", "echo");
                url += context.Request.QueryString.ToString();
                context.Response.Redirect(url, false, true);
                return Task.CompletedTask;
            }

            return next.Invoke(context);
        });

        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/redirect"))
            {
                var newUrl = context.Request.Query["baseUrl"] + "/testHub?numRedirects=" + Interlocked.Increment(ref _numRedirects);
                return context.Response.WriteAsync($"{{ \"url\": \"{newUrl}\" }}");
            }

            return next(context);
        });

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Value.Contains("/negotiate"))
            {
                var cookieOptions = new CookieOptions();
                var expiredCookieOptions = new CookieOptions() { Expires = DateTimeOffset.Now.AddHours(-1) };
                if (context.Request.IsHttps)
                {
                    cookieOptions.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    cookieOptions.Secure = true;
                    cookieOptions.Extensions.Add("partitioned"); // Required by Chromium

                    expiredCookieOptions.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                    expiredCookieOptions.Secure = true;
                    expiredCookieOptions.Extensions.Add("partitioned"); // Required by Chromium
                }
                context.Response.Cookies.Append("testCookie", "testValue", cookieOptions);
                context.Response.Cookies.Append("testCookie2", "testValue2", cookieOptions);

                cookieOptions.Expires = DateTimeOffset.Now.AddHours(-1);
                context.Response.Cookies.Append("expiredCookie", "doesntmatter", expiredCookieOptions);
            }

            await next.Invoke(context);
        });

        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/bad-negotiate"))
            {
                context.Response.StatusCode = 400;
                return context.Response.WriteAsync("Some response from server");
            }

            return next(context);
        });

        app.UseRouting();

        // Custom CORS to allow any origin + credentials (which isn't allowed by the CORS spec)
        // This is for testing purposes only (karma hosts the client on its own server), never do this in production
        app.UseCors(policy =>
        {
            policy.SetIsOriginAllowed(host =>
                host.StartsWith("http://localhost:", StringComparison.Ordinal)
                || host.StartsWith("http://127.0.0.1:", StringComparison.Ordinal)
                || host.StartsWith("https://localhost:", StringComparison.Ordinal)
                || host.StartsWith("https://127.0.0.1:", StringComparison.Ordinal))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<TestHub>("/testhub");
            endpoints.MapHub<TestHub>("/testhub-nowebsockets", options => options.Transports = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling);
            endpoints.MapHub<UncreatableHub>("/uncreatable");
            endpoints.MapHub<HubWithAuthorization>("/authorizedhub");

            endpoints.MapConnectionHandler<EchoConnectionHandler>("/echo");

            endpoints.MapGet("/generateJwtToken", context =>
            {
                return context.Response.WriteAsync(GenerateJwtToken());
            });

            endpoints.MapGet("/clientresult/{id}", async (IHubContext<TestHub> hubContext, string id) =>
            {
                try
                {
                    var result = await hubContext.Clients.Client(id).InvokeAsync<int>("Result", cancellationToken: default);
                    return result.ToString(CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            });

            endpoints.MapGet("/deployment", context =>
            {
                var attributes = Assembly.GetAssembly(typeof(Startup)).GetCustomAttributes<AssemblyMetadataAttribute>();

                context.Response.ContentType = "application/json";
                using (var textWriter = new StreamWriter(context.Response.Body))
                using (var writer = new JsonTextWriter(textWriter))
                {
                    var json = new JObject();
                    var commitHash = string.Empty;

                    foreach (var attribute in attributes)
                    {
                        json.Add(attribute.Key, attribute.Value);

                        if (string.Equals(attribute.Key, "CommitHash"))
                        {
                            commitHash = attribute.Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(commitHash))
                    {
                        json.Add("GitHubUrl", $"https://github.com/aspnet/SignalR/commit/{commitHash}");
                    }

                    json.WriteTo(writer);
                }

                return Task.CompletedTask;
            });
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
