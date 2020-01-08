// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class Http2Tests
    {
        [ConditionalFact(Skip = "https://github.com/dotnet/aspnetcore/issues/17420")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H1, SkipReason = "This is last version without GoAway support")]
        public async Task ConnectionClose_NoOSSupport_NoGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, endStream: true, decodedHeaders =>
                    {
                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    // Send and receive a second request to ensure there is no GoAway frame on the wire yet.

                    await h2Connection.StartStreamAsync(3, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(3, endStream: true, decodedHeaders =>
                    {
                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    await h2Connection.StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2, SkipReason = "GoAway support was added in Win10_19H2.")]
        public async Task ConnectionClose_OSSupport_SendsGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, int.MaxValue, Http2ErrorCode.NO_ERROR);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    // Http.Sys doesn't send a final GoAway unless we ignore the first one and send 200 additional streams.

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2, SkipReason = "GoAway support was added in Win10_19H2.")]
        public async Task ConnectionClose_AdditionalRequests_ReceivesSecondGoAway()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                httpContext.Response.Headers[HeaderNames.Connection] = "close";
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var streamId = 1;
                    await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, int.MaxValue, Http2ErrorCode.NO_ERROR);

                    await h2Connection.ReceiveHeadersAsync(streamId, decodedHeaders =>
                    {
                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, streamId, endOfStream: true, length: 0);

                    // Http.Sys doesn't send a final GoAway unless we ignore the first one and send 200 additional streams.

                    for (var i = 1; i < 200; i++)
                    {
                        streamId = 1 + (i * 2); // Odds.
                        await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                        await h2Connection.ReceiveHeadersAsync(streamId, decodedHeaders =>
                        {
                            // HTTP/2 filters out the connection header
                            Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                            Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                        });

                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Http2Utilities.VerifyDataFrame(dataFrame, streamId, endOfStream: true, length: 0);
                    }

                    streamId = 1 + (200 * 2); // Odds.
                    await h2Connection.StartStreamAsync(streamId, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    // Final GoAway
                    goAwayFrame = await h2Connection.ReceiveFrameAsync();
                    h2Connection.VerifyGoAway(goAwayFrame, streamId, Http2ErrorCode.NO_ERROR);

                    // Normal response
                    await h2Connection.ReceiveHeadersAsync(streamId, decodedHeaders =>
                    {
                        // HTTP/2 filters out the connection header
                        Assert.False(decodedHeaders.ContainsKey(HeaderNames.Connection));
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, streamId, endOfStream: true, length: 0);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        public async Task AppException_BeforeHeaders_500()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                throw new Exception("Application exception");
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("500", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "This is last version without custom Reset support")]
        public async Task AppException_AfterHeaders_PriorOSVersions_ResetCancel()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                await httpContext.Response.Body.FlushAsync();
                throw new Exception("Application exception");
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, Http2ErrorCode.CANCEL);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Custom Reset support was added in Win10_20H2.")]
        public async Task AppException_AfterHeaders_ResetInternalError()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                await httpContext.Response.Body.FlushAsync();
                throw new Exception("Application exception");
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, Http2ErrorCode.INTERNAL_ERROR);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        public async Task Reset_Http1_NotSupported()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                Assert.Equal("HTTP/1.1", httpContext.Request.Protocol);
                var feature = httpContext.Features.Get<IHttpResetFeature>();
                Assert.Null(feature);
                return httpContext.Response.WriteAsync("Hello World");
            });

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestVersion = HttpVersion.Version11;
            var response = await client.GetStringAsync(address);
            Assert.Equal("Hello World", response);
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "This is last version without Reset support")]
        public async Task Reset_PriorOSVersions_NotSupported()
        {
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                var feature = httpContext.Features.Get<IHttpResetFeature>();
                Assert.Null(feature);
                return httpContext.Response.WriteAsync("Hello World");
            });

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestVersion = HttpVersion.Version20;
            var response = await client.GetStringAsync(address);
            Assert.Equal("Hello World", response);
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_BeforeResponse_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);
                    feature.Reset(1111); // Custom
                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
                return Task.FromResult(0);
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_AfterResponseHeaders_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);
                    await httpContext.Response.Body.FlushAsync();
                    feature.Reset(1111); // Custom
                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_DurringResponseBody_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);
                    await httpContext.Response.WriteAsync("Hello World");
                    feature.Reset(1111); // Custom
                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 11);

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_AfterCompleteAsync_NoReset()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);
                    await httpContext.Response.WriteAsync("Hello World");
                    await httpContext.Response.CompleteAsync();
                    // The request and response are fully complete, the reset doesn't get sent.
                    feature.Reset(1111);
                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.BrowserRequestHeaders, endStream: true);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 11);

                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_BeforeRequestBody_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);
                    var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);

                    feature.Reset(1111);

                    await Assert.ThrowsAsync<IOException>(() => readTask);

                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.PostRequestHeaders, endStream: false);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_DurringRequestBody_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);

                    var read = await httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
                    Assert.Equal(10, read);

                    var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
                    feature.Reset(1111);
                    await Assert.ThrowsAsync<IOException>(() => readTask);

                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.PostRequestHeaders, endStream: false);
                    await h2Connection.SendDataAsync(1, new byte[10], endStream: false);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: (Http2ErrorCode)1111);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Reset support was added in Win10_20H2.")]
        public async Task Reset_CompleteAsyncDurringRequestBody_Resets()
        {
            var appResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var server = Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    Assert.Equal("HTTP/2", httpContext.Request.Protocol);
                    var feature = httpContext.Features.Get<IHttpResetFeature>();
                    Assert.NotNull(feature);

                    var read = await httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
                    Assert.Equal(10, read);

                    var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
                    await httpContext.Response.CompleteAsync();
                    feature.Reset((int)Http2ErrorCode.NO_ERROR); // GRPC does this
                    await Assert.ThrowsAsync<IOException>(() => readTask);

                    appResult.SetResult(0);
                }
                catch (Exception ex)
                {
                    appResult.SetException(ex);
                }
            });

            await new HostBuilder()
                .UseHttp2Cat(address, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, Http2Utilities.PostRequestHeaders, endStream: false);
                    await h2Connection.SendDataAsync(1, new byte[10], endStream: false);

                    // Any app errors?
                    Assert.Equal(0, await appResult.Task.DefaultTimeout());

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    var resetFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyResetFrame(resetFrame, expectedStreamId: 1, expectedErrorCode: Http2ErrorCode.NO_ERROR);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }
    }
}
