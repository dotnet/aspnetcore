// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ResponseBodyTests : LoggedTest
{
    [ConditionalFact]
    public async Task ResponseBody_StartAsync_LocksHeadersAndTriggersOnStarting()
    {
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            httpContext.Response.OnStarting(() =>
            {
                startingTcs.SetResult();
                return Task.CompletedTask;
            });
            await httpContext.Response.StartAsync();
            Assert.True(httpContext.Response.HasStarted);
            Assert.True(httpContext.Response.Headers.IsReadOnly);
            await startingTcs.Task.DefaultTimeout();
            await httpContext.Response.WriteAsync("Hello World");
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_CompleteAsync_TriggersOnStartingAndLocksHeaders()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            httpContext.Response.OnStarting(() =>
            {
                startingTcs.SetResult();
                return Task.CompletedTask;
            });
            await httpContext.Response.CompleteAsync();
            Assert.True(httpContext.Response.HasStarted);
            Assert.True(httpContext.Response.Headers.IsReadOnly);
            await startingTcs.Task.DefaultTimeout();
            await responseReceived.Task.DefaultTimeout();
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            responseReceived.SetResult();
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_CompleteAsync_FlushesThePipe()
    {
        var responseReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var writer = httpContext.Response.BodyWriter;
            var memory = writer.GetMemory();
            writer.Advance(memory.Length);
            await httpContext.Response.CompleteAsync();
            await responseReceived.Task.DefaultTimeout();
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(0 < (await response.Content.ReadAsByteArrayAsync()).Length);
            responseReceived.SetResult();
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_PipeAdapter_AutomaticallyFlushed()
    {
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            var writer = httpContext.Response.BodyWriter;
            var memory = writer.GetMemory();
            writer.Advance(memory.Length);
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(0 < (await response.Content.ReadAsByteArrayAsync()).Length);
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteNoHeaders_SetsChunked()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            httpContext.Response.Body.Write(new byte[10], 0, 10);
            return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResponseBody_WriteNoHeaders_SetsChunked_LargeBody(bool enableKernelBuffering)
    {
        const int WriteSize = 1024 * 1024;
        const int NumWrites = 32;

        string address;
        using (Utilities.CreateHttpServer(
            baseAddress: out address,
            configureOptions: options => { options.EnableKernelResponseBuffering = enableKernelBuffering; },
            app: async httpContext =>
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                for (int i = 0; i < NumWrites - 1; i++)
                {
                    httpContext.Response.Body.Write(new byte[WriteSize], 0, WriteSize);
                }
                await httpContext.Response.Body.WriteAsync(new byte[WriteSize], 0, WriteSize);
            }, loggerFactory: LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(WriteSize * NumWrites, bytes.Length);
            Assert.True(bytes.All(b => b == 0));
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteNoHeadersAndFlush_DefaultsToChunked()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            httpContext.Response.Body.Write(new byte[10], 0, 10);
            await httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            await httpContext.Response.Body.FlushAsync();
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
            Assert.Equal(new byte[20], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteChunked_ManuallyChunked()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            httpContext.Response.Headers["transfeR-Encoding"] = "CHunked";
            Stream stream = httpContext.Response.Body;
            var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
            Assert.Equal("Manually Chunked", await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteContentLength_PassedThrough()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            httpContext.Response.Headers["Content-lenGth"] = " 30 ";
            Stream stream = httpContext.Response.Body;
            stream.EndWrite(stream.BeginWrite(new byte[10], 0, 10, null, null));
            stream.Write(new byte[10], 0, 10);
            await stream.WriteAsync(new byte[10], 0, 10);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> contentLength;
            Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
            Assert.Equal("30", contentLength.First());
            Assert.Null(response.Headers.TransferEncodingChunked);
            Assert.Equal(new byte[30], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteContentLengthNoneWritten_Throws()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.Headers["Content-lenGth"] = " 20 ";
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteContentLengthNotEnoughWritten_Throws()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.Headers["Content-lenGth"] = " 20 ";
            return httpContext.Response.Body.WriteAsync(new byte[5], 0, 5);
        }, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(async () => await SendRequestAsync(address));
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteContentLengthTooMuchWritten_Throws()
    {
        var completed = false;
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            httpContext.Response.Headers["Content-lenGth"] = " 10 ";
            await httpContext.Response.Body.WriteAsync(new byte[5], 0, 5);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                httpContext.Response.Body.WriteAsync(new byte[6], 0, 6));
            completed = true;
        }, LoggerFactory))
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
            Assert.True(completed);
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteContentLengthExtraWritten_Throws()
    {
        var requestThrew = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            try
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                httpContext.Response.Headers["Content-lenGth"] = " 10 ";
                httpContext.Response.Body.Write(new byte[10], 0, 10);
                httpContext.Response.Body.Write(new byte[9], 0, 9);
                requestThrew.SetResult(false);
            }
            catch (Exception)
            {
                requestThrew.SetResult(true);
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            // The full response is received.
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            IEnumerable<string> contentLength;
            Assert.True(response.Content.Headers.TryGetValues("content-length", out contentLength), "Content-Length");
            Assert.Equal("10", contentLength.First());
            Assert.Null(response.Headers.TransferEncodingChunked);
            Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

            Assert.True(await requestThrew.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_Write_TriggersOnStarting()
    {
        var onStartingCalled = false;
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            httpContext.Response.OnStarting(state =>
            {
                onStartingCalled = true;
                Assert.Same(state, httpContext);
                return Task.FromResult(0);
            }, httpContext);
            httpContext.Response.Body.Write(new byte[10], 0, 10);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(onStartingCalled);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_BeginWrite_TriggersOnStarting()
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
            httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(new byte[10], 0, 10, null, null));
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(onStartingCalled);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalFact]
    public async Task ResponseBody_WriteAsync_TriggersOnStarting()
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
            return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(onStartingCalled);
            IEnumerable<string> ignored;
            Assert.False(response.Content.Headers.TryGetValues("content-length", out ignored), "Content-Length");
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
        }
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResponseBody_ZeroLengthTrailingWrite_Success(bool setContentLength)
    {
        string address;
        var completion = new TaskCompletionSource<bool>();
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var data = Encoding.UTF8.GetBytes("hello, world");
            if (setContentLength)
            {
                httpContext.Response.ContentLength = data.Length;
            }
            var body = httpContext.Response.Body;
            await body.WriteAsync(data);
            try
            {
                await body.FlushAsync();
                await body.WriteAsync(Array.Empty<byte>());
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                // in content-length scenarios, server-side faults after
                // the payload would not be observed
                completion.TrySetException(ex);
            }
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            var payload = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal("hello, world", Encoding.UTF8.GetString(payload));
        }

        await completion.Task; // also checks no-fault
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetAsync(uri);
        }
    }
}
