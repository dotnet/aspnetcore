// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class ResponseSendFileTests
    {
        private const string Address = "http://localhost:8080/";
        private readonly string AbsoluteFilePath;
        private readonly string RelativeFilePath;
        private readonly long FileLength;
        
        public ResponseSendFileTests()
        {
            AbsoluteFilePath = Directory.GetFiles(Environment.CurrentDirectory).First();
            RelativeFilePath = Path.GetFileName(AbsoluteFilePath);
            FileLength = new FileInfo(AbsoluteFilePath).Length;
        }

        [Fact]
        public async Task ResponseSendFile_MissingFile_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await Assert.ThrowsAsync<FileNotFoundException>(() => 
                    context.Response.SendFileAsync("Missing.txt", 0, null, CancellationToken.None));
                context.Dispose();
                
                HttpResponseMessage response = await responseTask;
            }
        }
        
        [Fact]
        public async Task ResponseSendFile_NoHeaders_DefaultsToChunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_RelativeFile_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await context.Response.SendFileAsync(RelativeFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> ignored;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_Chunked_Chunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Transfer-EncodinG"] = new[] { "CHUNKED" };
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_MultipleChunks_Chunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Transfer-EncodinG"] = new[] { "CHUNKED" };
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength * 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_ChunkedHalfOfFile_Chunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, FileLength / 2, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(FileLength / 2, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_ChunkedOffsetOutOfRange_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                    () => context.Response.SendFileAsync(AbsoluteFilePath, 1234567, null, CancellationToken.None));
                context.Dispose();

                HttpResponseMessage response = await responseTask;
            }
        }

        [Fact]
        public async Task ResponseSendFile_ChunkedCountOutOfRange_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                    () => context.Response.SendFileAsync(AbsoluteFilePath, 0, 1234567, CancellationToken.None));
                context.Dispose();

                HttpResponseMessage response = await responseTask;
            }
        }

        [Fact]
        public async Task ResponseSendFile_ChunkedCount0_Chunked()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.False(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.True(response.Headers.TransferEncodingChunked.Value);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_ContentLength_PassedThrough()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { FileLength.ToString() };
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, null, CancellationToken.None);

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal(FileLength.ToString(), contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(FileLength, response.Content.ReadAsByteArrayAsync().Result.Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_ContentLengthSpecific_PassedThrough()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { "10" };
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 10, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("10", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(10, (await response.Content.ReadAsByteArrayAsync()).Length);
            }
        }

        [Fact]
        public async Task ResponseSendFile_ContentLength0_PassedThrough()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.Headers["Content-lenGth"] = new[] { "0" };
                await context.Response.SendFileAsync(AbsoluteFilePath, 0, 0, CancellationToken.None);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                IEnumerable<string> contentLength;
                Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
                Assert.Equal("0", contentLength.First());
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
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