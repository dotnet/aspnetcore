// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class ResponseSendFileTests
    {
        private readonly string AbsoluteFilePath;
        private readonly string RelativeFilePath;
        private readonly long FileLength;

        public ResponseSendFileTests()
        {
            AbsoluteFilePath = Directory.GetFiles(Directory.GetCurrentDirectory()).First();
            RelativeFilePath = Path.GetFileName(AbsoluteFilePath);
            FileLength = new FileInfo(AbsoluteFilePath).Length;
        }

        [ConditionalFact]
        public async Task ResponseSendFile_SupportKeys_Present()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                try
                {
                    /* TODO:
                    IDictionary<string, object> capabilities = httpContext.Get<IDictionary<string, object>>("server.Capabilities");
                    Assert.NotNull(capabilities);

                    Assert.Equal("1.0", capabilities.Get<string>("sendfile.Version"));

                    IDictionary<string, object> support = capabilities.Get<IDictionary<string, object>>("sendfile.Support");
                    Assert.NotNull(support);

                    Assert.Equal("Overlapped", support.Get<string>("sendfile.Concurrency"));
                    */

                    var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                    Assert.NotNull(sendFile);
                }
                catch (Exception ex)
                {
                    byte[] body = Encoding.UTF8.GetBytes(ex.ToString());
                    httpContext.Response.Body.Write(body, 0, body.Length);
                }
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(0, response.Content.Headers.ContentLength);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_MissingFile_Throws()
        {
            var waitHandle = new ManualResetEvent(false);
            bool? appThrew = null;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                try
                {
                    sendFile.SendFileAsync(string.Empty, 0, null, CancellationToken.None).Wait();
                    appThrew = false;
                }
                catch (Exception)
                {
                    appThrew = true;
                    throw;
                }
                finally
                {
                    waitHandle.Set();
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.True(waitHandle.WaitOne(100));
                Assert.True(appThrew.HasValue, "appThrew.HasValue");
                Assert.True(appThrew.Value, "appThrew.Value");
            }
        }
        
        [ConditionalFact]
        public async Task ResponseSendFile_NoHeaders_DefaultsToChunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_RelativeFile_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(RelativeFilePath, 0, null, CancellationToken.None);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_Unspecified_Chunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_MultipleWrites_Chunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None).Wait();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_HalfOfFile_Chunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, FileLength / 2, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength / 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_OffsetOutOfRange_Throws()
        {
            var completed = false;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                    sendFile.SendFileAsync(AbsoluteFilePath, 1234567, null, CancellationToken.None));
                completed = true;
            }))
            {
                var response = await SendRequestAsync(address);
                response.EnsureSuccessStatusCode();
                Assert.True(completed);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_CountOutOfRange_Throws()
        {
            var completed = false;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                    sendFile.SendFileAsync(AbsoluteFilePath, 0, 1234567, CancellationToken.None));
                completed = true;
            }))
            {
                var response = await SendRequestAsync(address);
                response.EnsureSuccessStatusCode();
                Assert.True(completed);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_Count0_Chunked()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLength_PassedThrough()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                httpContext.Response.Headers["Content-lenGth"] = FileLength.ToString();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal(FileLength.ToString(), contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLengthSpecific_PassedThrough()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                httpContext.Response.Headers["Content-lenGth"] = "10";
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_ContentLength0_PassedThrough()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                httpContext.Response.Headers["Content-lenGth"] = "0";
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("0", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
            }
        }

        [ConditionalFact]
        public async Task ResponseSendFile_TriggersOnStarting()
        {
            var onStartingCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                var sendFile = httpContext.Features.Get<IHttpSendFileFeature>();
                return sendFile.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(onStartingCalled);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
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
