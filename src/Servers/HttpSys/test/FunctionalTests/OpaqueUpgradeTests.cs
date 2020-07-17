// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class OpaqueUpgradeTests
    {
        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        public async Task OpaqueUpgrade_DownLevel_FeatureIsAbsent()
        {
            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                try
                {
                    var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                    Assert.Null(opaqueFeature);
                }
                catch (Exception ex)
                {
                    return httpContext.Response.WriteAsync(ex.ToString());
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(0, response.Content.Headers.ContentLength);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task OpaqueUpgrade_SupportKeys_Present()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                try
                {
                    var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                }
                catch (Exception ex)
                {
                    return httpContext.Response.WriteAsync(ex.ToString());
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(0, response.Content.Headers.ContentLength);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task OpaqueUpgrade_AfterHeadersSent_Throws()
        {
            bool? upgradeThrew = null;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                await httpContext.Response.WriteAsync("Hello World");
                await httpContext.Response.Body.FlushAsync();
                try
                {
                    var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    await opaqueFeature.UpgradeAsync();
                    upgradeThrew = false;
                }
                catch (InvalidOperationException)
                {
                    upgradeThrew = true;
                }
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.True(upgradeThrew.Value);
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task OpaqueUpgrade_GetUpgrade_Success()
        {
            var upgraded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                Assert.NotNull(opaqueFeature);
                Assert.True(opaqueFeature.IsUpgradableRequest);
                await opaqueFeature.UpgradeAsync();
                upgraded.SetResult(true);
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync("GET", address))
                {
                    Assert.True(await upgraded.Task.TimeoutAfter(TimeSpan.FromSeconds(1)));
                }
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task OpaqueUpgrade_GetUpgrade_NotAffectedByMaxRequestBodyLimit()
        {
            var upgraded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
                Assert.NotNull(feature);
                Assert.False(feature.IsReadOnly);
                Assert.Null(feature.MaxRequestBodySize); // GET/Upgrade requests don't actually have an entity body, so they can't set the limit.

                httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                Assert.NotNull(opaqueFeature);
                Assert.True(opaqueFeature.IsUpgradableRequest);
                var stream = await opaqueFeature.UpgradeAsync();
                Assert.True(feature.IsReadOnly);
                Assert.Null(feature.MaxRequestBodySize);
                Assert.Throws<InvalidOperationException>(() => feature.MaxRequestBodySize = 12);
                Assert.Equal(15, await stream.ReadAsync(new byte[15], 0, 15));
                upgraded.SetResult(true);
            }, options => options.MaxRequestBodySize = 10))
            {
                using (Stream stream = await SendOpaqueRequestAsync("GET", address))
                {
                    stream.Write(new byte[15], 0, 15);
                    Assert.True(await upgraded.Task.TimeoutAfter(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task OpaqueUpgrade_WithOnStarting_CallbackCalled()
        {
            var callbackCalled = false;
            var upgraded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                httpContext.Response.OnStarting(_ =>
                {
                    callbackCalled = true;
                    return Task.FromResult(0);
                }, null);
                httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                Assert.NotNull(opaqueFeature);
                Assert.True(opaqueFeature.IsUpgradableRequest);
                await opaqueFeature.UpgradeAsync();
                upgraded.SetResult(true);
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync("GET", address))
                {
                    Assert.True(await upgraded.Task.TimeoutAfter(TimeSpan.FromSeconds(1)));
                    Assert.True(callbackCalled, "Callback not called");
                }
            }
        }

        [ConditionalTheory]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        // See HTTP_VERB for known verbs
        [InlineData("UNKNOWN", null)]
        [InlineData("INVALID", null)]
        [InlineData("OPTIONS", null)]
        [InlineData("GET", null)]
        [InlineData("HEAD", null)]
        [InlineData("DELETE", null)]
        [InlineData("TRACE", null)]
        [InlineData("CONNECT", null)]
        [InlineData("TRACK", null)]
        [InlineData("MOVE", null)]
        [InlineData("COPY", null)]
        [InlineData("PROPFIND", null)]
        [InlineData("PROPPATCH", null)]
        [InlineData("MKCOL", null)]
        [InlineData("LOCK", null)]
        [InlineData("UNLOCK", null)]
        [InlineData("SEARCH", null)]
        [InlineData("CUSTOMVERB", null)]
        [InlineData("PATCH", null)]
        [InlineData("POST", "Content-Length: 0")]
        [InlineData("PUT", "Content-Length: 0")]
        public async Task OpaqueUpgrade_VariousMethodsUpgradeSendAndReceive_Success(string method, string extraHeader)
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                try
                {
                    httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                    var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    Assert.True(opaqueFeature.IsUpgradableRequest);
                    var opaqueStream = await opaqueFeature.UpgradeAsync();

                    byte[] buffer = new byte[100];
                    int read = await opaqueStream.ReadAsync(buffer, 0, buffer.Length);

                    await opaqueStream.WriteAsync(buffer, 0, read);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync(method, address, extraHeader))
                {
                    byte[] data = new byte[100];
                    await stream.WriteAsync(data, 0, 49);
                    int read = await stream.ReadAsync(data, 0, data.Length);
                    Assert.Equal(49, read);
                }
            }
        }

        [ConditionalTheory]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        // Http.Sys returns a 411 Length Required if PUT or POST does not specify content-length or chunked.
        [InlineData("POST", "Content-Length: 10")]
        [InlineData("POST", "Transfer-Encoding: chunked")]
        [InlineData("PUT", "Content-Length: 10")]
        [InlineData("PUT", "Transfer-Encoding: chunked")]
        [InlineData("CUSTOMVERB", "Content-Length: 10")]
        [InlineData("CUSTOMVERB", "Transfer-Encoding: chunked")]
        public async Task OpaqueUpgrade_InvalidMethodUpgrade_Disconnected(string method, string extraHeader)
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                try
                {
                    var opaqueFeature = httpContext.Features.Get<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    Assert.False(opaqueFeature.IsUpgradableRequest);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendOpaqueRequestAsync(method, address, extraHeader));
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }

        // Returns a bidirectional opaque stream or throws if the upgrade fails
        private async Task<Stream> SendOpaqueRequestAsync(string method, string address, string extraHeader = null)
        {
            // Connect with a socket
            Uri uri = new Uri(address);
            TcpClient client = new TcpClient();

            try
            {
                await client.ConnectAsync(uri.Host, uri.Port);
                NetworkStream stream = client.GetStream();

                // Send an HTTP GET request
                byte[] requestBytes = BuildGetRequest(method, uri, extraHeader);
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                // Read the response headers, fail if it's not a 101
                await ParseResponseAsync(stream);

                // Return the opaque network stream
                return stream;
            }
            catch (Exception)
            {
                ((IDisposable)client).Dispose();
                throw;
            }
        }

        private byte[] BuildGetRequest(string method, Uri uri, string extraHeader)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(method);
            builder.Append(" ");
            builder.Append(uri.PathAndQuery);
            builder.Append(" HTTP/1.1");
            builder.AppendLine();

            builder.Append("Host: ");
            builder.Append(uri.Host);
            builder.Append(':');
            builder.Append(uri.Port);
            builder.AppendLine();

            if (!string.IsNullOrEmpty(extraHeader))
            {
                builder.AppendLine(extraHeader);
            }

            builder.AppendLine();
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        // Read the response headers, fail if it's not a 101
        private async Task ParseResponseAsync(NetworkStream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string statusLine = await reader.ReadLineAsync();
            string[] parts = statusLine.Split(' ');
            if (int.Parse(parts[1]) != 101)
            {
                throw new InvalidOperationException("The response status code was incorrect: " + statusLine);
            }

            // Scan to the end of the headers
            while (!string.IsNullOrEmpty(reader.ReadLine()))
            {
            }
        }
    }
}
