// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace IIS.Tests
{
    [MsQuicSupported]
    public class Http3Tests : LoggedTest
    {
        [ConditionalFact]
        public async Task Http3_Direct()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    try
                    {
                        Assert.True(ctx.Request.IsHttps);
                        await ctx.Response.WriteAsync(ctx.Request.Protocol);
                    }
                    catch (Exception ex)
                    {
                        await ctx.Response.WriteAsync(ex.ToString());
                    }
                }, LoggerFactory))
            {
                var handler = new HttpClientHandler();
                // Needed on CI, the IIS Express cert we use isn't trusted there.
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using var client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version30;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                var response = await client.GetStringAsync(testServer.HttpClient.BaseAddress);
                Assert.Equal("HTTP/3", response);
            }
        }
    }
}
