// -----------------------------------------------------------------------
// <copyright file="ResponseBodyTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ResponseBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task ResponseBody_WriteNoHeaders_DefaultsToChunked()
        {
            using (CreateServer(env =>
            {
                env.Get<Stream>("owin.ResponseBody").Write(new byte[10], 0, 10);
                return env.Get<Stream>("owin.ResponseBody").WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteChunked_Chunked()
        {
            using (CreateServer(env =>
            {
                env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["transfeR-Encoding"] = new string[] { " CHunked " };
                Stream stream = env.Get<Stream>("owin.ResponseBody");
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                return stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLength_PassedThrough()
        {
            using (CreateServer(env =>
            {
                env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["Content-lenGth"] = new string[] { " 30 " };
                Stream stream = env.Get<Stream>("owin.ResponseBody");
                stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
                stream.Write(new byte[10], 0, 10);
                return stream.WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("30", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public async Task ResponseBody_Http10WriteNoHeaders_DefaultsConnectionClose()
        {
            using (CreateServer(env =>
            {
                env["owin.ResponseProtocol"] = "HTTP/1.0";
                env.Get<Stream>("owin.ResponseBody").Write(new byte[10], 0, 10);
                return env.Get<Stream>("owin.ResponseBody").WriteAsync(new byte[10], 0, 10);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version); // Http.Sys won't transmit 1.0
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [Fact]
        public void ResponseBody_WriteContentLengthNoneWritten_Throws()
        {
            using (CreateServer(env =>
            {
                env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["Content-lenGth"] = new string[] { " 20 " };
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(Address).Result);
            }
        }

        [Fact]
        public void ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
        {
            using (CreateServer(env =>
            {
                env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["Content-lenGth"] = new string[] { " 20 " };
                env.Get<Stream>("owin.ResponseBody").Write(new byte[5], 0, 5);
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(Address).Result);
            }
        }

        [Fact]
        public void ResponseBody_WriteContentLengthTooMuchWritten_Throws()
        {
            using (CreateServer(env =>
            {
                env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["Content-lenGth"] = new string[] { " 10 " };
                env.Get<Stream>("owin.ResponseBody").Write(new byte[5], 0, 5);
                env.Get<Stream>("owin.ResponseBody").Write(new byte[6], 0, 6);
                return Task.FromResult(0);
            }))
            {
                Assert.Throws<AggregateException>(() => SendRequestAsync(Address).Result);
            }
        }

        [Fact]
        public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            bool? appThrew = null;
            using (CreateServer(env =>
            {
                try
                {
                    env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders")["Content-lenGth"] = new string[] { " 10 " };
                    env.Get<Stream>("owin.ResponseBody").Write(new byte[10], 0, 10);
                    env.Get<Stream>("owin.ResponseBody").Write(new byte[9], 0, 9);
                    appThrew = false;
                }
                catch (Exception)
                {
                    appThrew = true;
                }
                waitHandle.Set();
                return Task.FromResult(0);
            }))
            {
                // The full response is received.
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

                Assert.True(waitHandle.WaitOne(100));
                Assert.True(appThrew.HasValue, "appThrew.HasValue");
                Assert.True(appThrew.Value, "appThrew.Value");
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

            return OwinServerFactory.Create(app, properties);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
