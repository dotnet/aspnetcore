// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace IISTestSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Map("/ServerVariable", ServerVariable);
            app.Map("/AuthenticationAnonymous", AuthenticationAnonymous);
            app.Map("/AuthenticationRestricted", AuthenticationRestricted);
            app.Map("/AuthenticationForbidden", AuthenticationForbidden);
            app.Map("/AuthenticationRestrictedNTLM", AuthenticationRestrictedNTLM);
            app.Map("/FeatureCollectionSetRequestFeatures", FeatureCollectionSetRequestFeatures);
            app.Map("/FeatureCollectionSetResponseFeatures", FeatureCollectionSetResponseFeatures);
            app.Map("/FeatureCollectionSetConnectionFeatures", FeatureCollectionSetConnectionFeatures);
            app.Map("/HelloWorld", HelloWorld);
            app.Map("/LargeResponseBody", LargeResponseBody);
            app.Map("/ResponseHeaders", ResponseHeaders);
            app.Map("/ResponseInvalidOrdering", ResponseInvalidOrdering);
            app.Map("/CheckEnvironmentVariable", CheckEnvironmentVariable);
            app.Map("/CheckEnvironmentLongValueVariable", CheckEnvironmentLongValueVariable);
            app.Map("/CheckAppendedEnvironmentVariable", CheckAppendedEnvironmentVariable);
            app.Map("/CheckRemoveAuthEnvironmentVariable", CheckRemoveAuthEnvironmentVariable);
        }

        private void ServerVariable(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                var varName = ctx.Request.Query["q"];
                await ctx.Response.WriteAsync($"{varName}: {ctx.GetIISServerVariable(varName) ?? "(null)"}");
            });
        }

        public void AuthenticationAnonymous(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                await ctx.Response.WriteAsync("Anonymous?" + !ctx.User.Identity.IsAuthenticated);
            });
        }

        private void AuthenticationRestricted(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (ctx.User.Identity.IsAuthenticated)
                {
                    await ctx.Response.WriteAsync(ctx.User.Identity.AuthenticationType);
                }
                else
                {
                    await ctx.ChallengeAsync(IISDefaults.AuthenticationScheme);
                }
            });
        }

        public void AuthenticationForbidden(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                await ctx.ForbidAsync(IISDefaults.AuthenticationScheme);
            });
        }

        public void AuthenticationRestrictedNTLM(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (string.Equals("NTLM", ctx.User.Identity.AuthenticationType, StringComparison.Ordinal))
                {
                    await ctx.Response.WriteAsync("NTLM");
                }
                else
                {
                    await ctx.ChallengeAsync(IISDefaults.AuthenticationScheme);
                }
            });
        }

        private void FeatureCollectionSetRequestFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.Equal("GET", context.Request.Method);
                    context.Request.Method = "test";
                    Assert.Equal("test", context.Request.Method);

                    Assert.Equal("http", context.Request.Scheme);
                    context.Request.Scheme = "test";
                    Assert.Equal("test", context.Request.Scheme);

                    Assert.Equal("/FeatureCollectionSetRequestFeatures", context.Request.PathBase);
                    context.Request.PathBase = "/base";
                    Assert.Equal("/base", context.Request.PathBase);

                    Assert.Equal("/path", context.Request.Path);
                    context.Request.Path = "/path";
                    Assert.Equal("/path", context.Request.Path);

                    Assert.Equal("?query", context.Request.QueryString.Value);
                    context.Request.QueryString = QueryString.Empty;
                    Assert.Equal("", context.Request.QueryString.Value);

                    Assert.Equal("HTTP/1.1", context.Request.Protocol);
                    context.Request.Protocol = "HTTP/1.0";
                    Assert.Equal("HTTP/1.0", context.Request.Protocol);

                    Assert.NotNull(context.Request.Headers);
                    var headers = new HeaderDictionary();
                    context.Features.Get<IHttpRequestFeature>().Headers = headers;
                    Assert.Same(headers, context.Features.Get<IHttpRequestFeature>().Headers);

                    Assert.NotNull(context.Request.Body);
                    var body = new MemoryStream();
                    context.Request.Body = body;
                    Assert.Same(body, context.Request.Body);

                    //Assert.NotNull(context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier);
                    //Assert.NotEqual(CancellationToken.None, context.RequestAborted);
                    //var token = new CancellationTokenSource().Token;
                    //context.RequestAborted = token;
                    //Assert.Equal(token, context.RequestAborted);

                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void FeatureCollectionSetResponseFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.Equal(200, context.Response.StatusCode);
                    context.Response.StatusCode = 404;
                    Assert.Equal(404, context.Response.StatusCode);
                    context.Response.StatusCode = 200;

                    Assert.Null(context.Features.Get<IHttpResponseFeature>().ReasonPhrase);
                    context.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Set Response";
                    Assert.Equal("Set Response", context.Features.Get<IHttpResponseFeature>().ReasonPhrase);

                    Assert.NotNull(context.Response.Headers);
                    var headers = new HeaderDictionary();
                    context.Features.Get<IHttpResponseFeature>().Headers = headers;
                    Assert.Same(headers, context.Features.Get<IHttpResponseFeature>().Headers);

                    var originalBody = context.Response.Body;
                    Assert.NotNull(originalBody);
                    var body = new MemoryStream();
                    context.Response.Body = body;
                    Assert.Same(body, context.Response.Body);
                    context.Response.Body = originalBody;

                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void FeatureCollectionSetConnectionFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.True(IPAddress.IsLoopback(context.Connection.LocalIpAddress));
                    context.Connection.LocalIpAddress = IPAddress.IPv6Any;
                    Assert.Equal(IPAddress.IPv6Any, context.Connection.LocalIpAddress);

                    Assert.True(IPAddress.IsLoopback(context.Connection.RemoteIpAddress));
                    context.Connection.RemoteIpAddress = IPAddress.IPv6Any;
                    Assert.Equal(IPAddress.IPv6Any, context.Connection.RemoteIpAddress);
                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void HelloWorld(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (ctx.Request.Path.Value.StartsWith("/Path"))
                {
                    await ctx.Response.WriteAsync(ctx.Request.Path.Value);
                    return;
                }
                if (ctx.Request.Path.Value.StartsWith("/Query"))
                {
                    await ctx.Response.WriteAsync(ctx.Request.QueryString.Value);
                    return;
                }

                await ctx.Response.WriteAsync("Hello World");
            });
        }

        private void LargeResponseBody(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (int.TryParse(context.Request.Query["length"], out var length))
                {
                    await context.Response.WriteAsync(new string('a', length));
                }
            });
        }

        private void ResponseHeaders(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.Headers["UnknownHeader"] = "test123=foo";
                context.Response.ContentType = "text/plain";
                context.Response.Headers["MultiHeader"] = new StringValues(new string[] { "1", "2" });
                await context.Response.WriteAsync("Request Complete");
            });
        }

        private void ResponseInvalidOrdering(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/SetStatusCodeAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.StatusCode = 200;
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetStatusCodeAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
                else if (context.Request.Path.Equals("/SetHeaderAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.Headers["This will fail"] = "some value";
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetHeaderAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
            });
        }

        private void CheckEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_INPROCESS_TESTING_VALUE");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckEnvironmentLongValueVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_INPROCESS_TESTING_LONG_VALUE");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckAppendedEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ProgramFiles");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckRemoveAuthEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH");
                await context.Response.WriteAsync(variable);
            });
        }
    }
}
