// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

/* TODO: Opaque
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.Net.Server
{
    using AppFunc = Func<object, Task>;
    using OpaqueUpgrade = Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>;

    public class OpaqueUpgradeTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task OpaqueUpgrade_SupportKeys_Present()
        {
            using (CreateServer(env =>
            {
                try
                {
                    IDictionary<string, object> capabilities = env.Get<IDictionary<string, object>>("server.Capabilities");
                    Assert.NotNull(capabilities);

                    Assert.Equal("1.0", capabilities.Get<string>("opaque.Version"));

                    OpaqueUpgrade opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                    Assert.NotNull(opaqueUpgrade);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.UTF8.GetBytes(ex.ToString());
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(0, response.Content.Headers.ContentLength);
                Assert.Equal(string.Empty, response.Content.ReadAsStringAsync().Result);
            }
        }

        [Fact]
        public async Task OpaqueUpgrade_NullCallback_Throws()
        {
            using (CreateServer(env =>
            {
                try
                {
                    OpaqueUpgrade opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                    opaqueUpgrade(new Dictionary<string, object>(), null);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.UTF8.GetBytes(ex.ToString());
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Contains("callback", response.Content.ReadAsStringAsync().Result);
            }
        }

        [Fact]
        public async Task OpaqueUpgrade_AfterHeadersSent_Throws()
        {
            bool? upgradeThrew = null;
            using (CreateServer(env =>
            {
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                OpaqueUpgrade opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                try
                {
                    opaqueUpgrade(null, _ => Task.FromResult(0));
                    upgradeThrew = false;
                }
                catch (InvalidOperationException)
                {
                    upgradeThrew = true;
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.True(upgradeThrew.Value);
            }
        }

        [Fact]
        public async Task OpaqueUpgrade_GetUpgrade_Success()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            bool? callbackInvoked = null;
            using (CreateServer(env =>
            {
                var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                responseHeaders["Upgrade"] = new string[] { "websocket" }; // Win8.1 blocks anything but WebSockets
                OpaqueUpgrade opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                opaqueUpgrade(null, opqEnv => 
                {
                    callbackInvoked = true;
                    waitHandle.Set();
                    return Task.FromResult(0);
                });
                return Task.FromResult(0);
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync("GET", Address))
                {
                    Assert.True(waitHandle.WaitOne(TimeSpan.FromSeconds(1)), "Timed out");
                    Assert.True(callbackInvoked.HasValue, "CallbackInvoked not set");
                    Assert.True(callbackInvoked.Value, "Callback not invoked");
                }
            }
        }

        [Theory]
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
            using (CreateServer(env =>
            {
                var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                responseHeaders["Upgrade"] = new string[] { "WebSocket" }; // Win8.1 blocks anything but WebSockets
                OpaqueUpgrade opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                opaqueUpgrade(null, async opqEnv =>
                {
                    Stream opaqueStream = opqEnv.Get<Stream>("opaque.Stream");

                    byte[] buffer = new byte[100];
                    int read = await opaqueStream.ReadAsync(buffer, 0, buffer.Length);

                    await opaqueStream.WriteAsync(buffer, 0, read);
                });
                return Task.FromResult(0);
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync(method, Address, extraHeader))
                {
                    byte[] data = new byte[100];
                    stream.WriteAsync(data, 0, 49).Wait();
                    int read = stream.ReadAsync(data, 0, data.Length).Result;
                    Assert.Equal(49, read);
                }
            }
        }

        [Theory]
        // Http.Sys returns a 411 Length Required if PUT or POST does not specify content-length or chunked.
        [InlineData("POST", "Content-Length: 10")]
        [InlineData("POST", "Transfer-Encoding: chunked")]
        [InlineData("PUT", "Content-Length: 10")]
        [InlineData("PUT", "Transfer-Encoding: chunked")]
        [InlineData("CUSTOMVERB", "Content-Length: 10")]
        [InlineData("CUSTOMVERB", "Transfer-Encoding: chunked")]
        public void OpaqueUpgrade_InvalidMethodUpgrade_Disconnected(string method, string extraHeader)
        {
            OpaqueUpgrade opaqueUpgrade = null;
            using (CreateServer(env =>
            {
                opaqueUpgrade = env.Get<OpaqueUpgrade>("opaque.Upgrade");
                if (opaqueUpgrade == null)
                {
                    throw new NotImplementedException();
                }
                opaqueUpgrade(null, opqEnv => Task.FromResult(0));
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    try
                    {
                        return SendOpaqueRequestAsync(method, Address, extraHeader).Result;
                    }
                    catch (AggregateException ag)
                    {
                        throw ag.GetBaseException();
                    }
                });
                Assert.Null(opaqueUpgrade);
            }
        }

        private IDisposable CreateServer(AppFunc app)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            IDictionary<string, object> address = new Dictionary<string, object>();
            addresses.Add(address);

            address["scheme"] = "http";
            address["host"] = "localhost";
            address["port"] = "8080";
            address["path"] = string.Empty;

            OwinServerFactory.Initialize(properties);

            return OwinServerFactory.Create(app, properties);
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
                client.Close();
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
*/