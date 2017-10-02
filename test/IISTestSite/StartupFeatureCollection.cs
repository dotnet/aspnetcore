// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IISTestSite
{
    public class StartupFeatureCollection
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async context =>
            {
                try
                {
                    // Verify setting and getting each feature/ portion of the httpcontext works
                    if (context.Request.Path.Equals("/SetRequestFeatures"))
                    {

                        Assert.Equal("GET", context.Request.Method);
                        context.Request.Method = "test";
                        Assert.Equal("test", context.Request.Method);

                        Assert.Equal("http", context.Request.Scheme);
                        context.Request.Scheme = "test";
                        Assert.Equal("test", context.Request.Scheme);

                        Assert.Equal("", context.Request.PathBase);
                        context.Request.PathBase = "/base";
                        Assert.Equal("/base", context.Request.PathBase);

                        Assert.Equal("/SetRequestFeatures", context.Request.Path);
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
                    else if (context.Request.Path.Equals("/SetResponseFeatures"))
                    {
                        Assert.Equal(200, context.Response.StatusCode);
                        context.Response.StatusCode = 404;
                        Assert.Equal(404, context.Response.StatusCode);

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
                    else if (context.Request.Path.Equals("/SetConnectionFeatures"))
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
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }
    }
}
