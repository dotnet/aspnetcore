// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
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
                }, LoggerFactory, useHttps: true))
            {
                var address = testServer.HttpClient.BaseAddress;
                testServer.HttpClient.Dispose();
                var handler = new HttpClientHandler();
                // Needed on CI, the IIS Express cert we use isn't trusted there.
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using var client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version30;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                var response = await client.GetStringAsync(address);
                Assert.Equal("HTTP/3", response);
            }
        }

        [ConditionalFact]
        public void Dummy()
        {
            Assert.True(true);
        }

        [ConditionalFact]
        public async Task Http3_AltSvcHeader_UpgradeFromHttp1()
        {
            var altsvc = "";
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    try
                    {
                        Assert.True(ctx.Request.IsHttps);
                        // Alt-Svc is not supported by Http.Sys, you need to add it yourself.
                        ctx.Response.Headers.AltSvc = altsvc;
                        await ctx.Response.WriteAsync(ctx.Request.Protocol);
                    }
                    catch (Exception ex)
                    {
                        await ctx.Response.WriteAsync(ex.ToString());
                    }
                }, LoggerFactory, useHttps: true))
            {
                var address = testServer.HttpClient.BaseAddress;
                testServer.HttpClient.Dispose();
                altsvc = $@"h3="":{address.Port}""";
                var handler = new HttpClientHandler();
                // Needed on CI, the IIS Express cert we use isn't trusted there.
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using var client = new HttpClient(handler);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                // First request is HTTP/1.1, gets an alt-svc response
                var request = new HttpRequestMessage(HttpMethod.Get, address);
                request.Version = HttpVersion.Version11;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                var response1 = await client.SendAsync(request);
                response1.EnsureSuccessStatusCode();
                Assert.Equal("HTTP/1.1", await response1.Content.ReadAsStringAsync());
                Assert.Equal(altsvc, response1.Headers.GetValues(HeaderNames.AltSvc).SingleOrDefault());

                // Second request is HTTP/3
                var response3 = await client.GetStringAsync(address);
                Assert.Equal("HTTP/3", response3);
            }
        }
    }
}
