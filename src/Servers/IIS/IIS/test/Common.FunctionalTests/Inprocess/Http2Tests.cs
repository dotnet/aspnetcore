// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http2Cat;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(PublishedSitesCollection.Name)]
    public class Http2Tests : IISFunctionalTestBase
    {
        // TODO: Remove when the regression is fixed.
        // https://github.com/dotnet/aspnetcore/issues/23164#issuecomment-652646163
        private static readonly Version Win10_Regressed_DataFrame = new Version(10, 0, 20145, 0);

        public static readonly IEnumerable<KeyValuePair<string, string>> Headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
            new KeyValuePair<string, string>("user-agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:54.0) Gecko/20100101 Firefox/54.0"),
            new KeyValuePair<string, string>("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
            new KeyValuePair<string, string>("accept-language", "en-US,en;q=0.5"),
            new KeyValuePair<string, string>("accept-encoding", "gzip, deflate, br"),
            new KeyValuePair<string, string>("upgrade-insecure-requests", "1"),
        };

        public Http2Tests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalTheory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("CUSTOM")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        public async Task Http2_MethodsRequestWithoutData_Success(string method)
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            await new HostBuilder()
                .UseHttp2Cat(deploymentResult.ApplicationBaseUri, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var headers = new[]
                    {
                        new KeyValuePair<string, string>(HeaderNames.Method, method),
                        new KeyValuePair<string, string>(HeaderNames.Path, "/Http2_MethodsRequestWithoutData_Success"),
                        new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
                        new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
                    };

                    await h2Connection.StartStreamAsync(1, headers, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    if (Environment.OSVersion.Version >= Win10_Regressed_DataFrame)
                    {
                        // TODO: Remove when the regression is fixed.
                        // https://github.com/dotnet/aspnetcore/issues/23164#issuecomment-652646163
                        Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 0);

                        dataFrame = await h2Connection.ReceiveFrameAsync();
                    }
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalTheory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Http2 requires Win10")]
        public async Task Http2_PostRequestWithoutData_LengthRequired(string method)
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            await new HostBuilder()
                .UseHttp2Cat(deploymentResult.ApplicationBaseUri, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var headers = new[]
                    {
                        new KeyValuePair<string, string>(HeaderNames.Method, method),
                        new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                        new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
                        new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
                    };

                    await h2Connection.StartStreamAsync(1, headers, endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("411", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: false, length: 344);
                    dataFrame = await h2Connection.ReceiveFrameAsync();
                    Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalTheory]
        [InlineData("GET")]
        // [InlineData("HEAD")] Reset with code HTTP_1_1_REQUIRED
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("CUSTOM")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
        public async Task Http2_RequestWithDataAndContentLength_Success(string method)
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            await new HostBuilder()
                .UseHttp2Cat(deploymentResult.ApplicationBaseUri, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var headers = new[]
                    {
                        new KeyValuePair<string, string>(HeaderNames.Method, method),
                        new KeyValuePair<string, string>(HeaderNames.Path, "/Http2_RequestWithDataAndContentLength_Success"),
                        new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
                        new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
                        new KeyValuePair<string, string>(HeaderNames.ContentLength, "11"),
                    };

                    await h2Connection.StartStreamAsync(1, headers, endStream: false);

                    await h2Connection.SendDataAsync(1, Encoding.UTF8.GetBytes("Hello World"), endStream: true);

                    // Http.Sys no longer sends a window update here on later versions.
                    if (Environment.OSVersion.Version < new Version(10, 0, 19041, 0))
                    {
                        var windowUpdate = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.WINDOW_UPDATE, windowUpdate.Type);
                    }

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);

                    // Some versions send an empty data frame first.
                    if (dataFrame.PayloadLength == 0)
                    {
                        Assert.False(dataFrame.DataEndStream);
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                        Assert.Equal(1, dataFrame.StreamId);
                    }

                    Assert.Equal(11, dataFrame.PayloadLength);
                    Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                    if (!dataFrame.DataEndStream)
                    {
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                    }

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalTheory]
        [InlineData("GET")]
        // [InlineData("HEAD")] Reset with code HTTP_1_1_REQUIRED
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("CUSTOM")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
        public async Task Http2_RequestWithDataAndNoContentLength_Success(string method)
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            await new HostBuilder()
                .UseHttp2Cat(deploymentResult.ApplicationBaseUri, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    var headers = new[]
                    {
                        new KeyValuePair<string, string>(HeaderNames.Method, method),
                        new KeyValuePair<string, string>(HeaderNames.Path, "/Http2_RequestWithDataAndNoContentLength_Success"),
                        new KeyValuePair<string, string>(HeaderNames.Scheme, "https"),
                        new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:443"),
                    };

                    await h2Connection.StartStreamAsync(1, headers, endStream: false);

                    await h2Connection.SendDataAsync(1, Encoding.UTF8.GetBytes("Hello World"), endStream: true);

                    // Http.Sys no longer sends a window update here on later versions.
                    if (Environment.OSVersion.Version < new Version(10, 0, 19041, 0))
                    {
                        var windowUpdate = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.WINDOW_UPDATE, windowUpdate.Type);
                    }

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);

                    // Some versions send an empty data frame first.
                    if (dataFrame.PayloadLength == 0)
                    {
                        Assert.False(dataFrame.DataEndStream);
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                        Assert.Equal(1, dataFrame.StreamId);
                    }

                    Assert.Equal(11, dataFrame.PayloadLength);
                    Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                    if (!dataFrame.DataEndStream)
                    {
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                    }

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H1, SkipReason = "Http2 requires Win10, and older versions of Win10 send some odd empty data frames.")]
        public async Task Http2_ResponseWithData_Success()
        {
            var deploymentParameters = GetHttpsDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            await new HostBuilder()
                .UseHttp2Cat(deploymentResult.ApplicationBaseUri, async h2Connection =>
                {
                    await h2Connection.InitializeConnectionAsync();

                    h2Connection.Logger.LogInformation("Initialized http2 connection. Starting stream 1.");

                    await h2Connection.StartStreamAsync(1, GetHeaders("/Http2_ResponseWithData_Success"), endStream: true);

                    await h2Connection.ReceiveHeadersAsync(1, decodedHeaders =>
                    {
                        Assert.Equal("200", decodedHeaders[HeaderNames.Status]);
                    });

                    var dataFrame = await h2Connection.ReceiveFrameAsync();
                    Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                    Assert.Equal(1, dataFrame.StreamId);

                    // Some versions send an empty data frame first.
                    if (dataFrame.PayloadLength == 0)
                    {
                        Assert.False(dataFrame.DataEndStream);
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Assert.Equal(Http2FrameType.DATA, dataFrame.Type);
                        Assert.Equal(1, dataFrame.StreamId);
                    }

                    Assert.Equal(11, dataFrame.PayloadLength);
                    Assert.Equal("Hello World", Encoding.UTF8.GetString(dataFrame.Payload.Span));

                    if (!dataFrame.DataEndStream)
                    {
                        dataFrame = await h2Connection.ReceiveFrameAsync();
                        Http2Utilities.VerifyDataFrame(dataFrame, 1, endOfStream: true, length: 0);
                    }

                    h2Connection.Logger.LogInformation("Connection stopped.");
                })
                .Build().RunAsync();
        }

        private static List<KeyValuePair<string, string>> GetHeaders(string path)
        {
            var headers = Headers.ToList();

            var kvp = new KeyValuePair<string, string>(HeaderNames.Path, path);
            headers.Add(kvp);
            return headers;
        }

        private IISDeploymentParameters GetHttpsDeploymentParameters()
        {
            var port = TestPortHelper.GetNextSSLPort();
            var deploymentParameters = Fixture.GetBaseDeploymentParameters();
            deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
            deploymentParameters.AddHttpsToServerConfig();
            return deploymentParameters;
        }
    }
}
