// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(IISSubAppSiteCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;" + "Windows.Amd64.VS2022.Pre;")]
public class RequestPathBaseTests : FixtureLoggedTest
{
    private readonly IISSubAppSiteFixture _fixture;

    public RequestPathBaseTests(IISSubAppSiteFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [ConditionalTheory]
    [SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;" + "Windows.Amd64.VS2022.Pre;")]
    [RequiresNewHandler]
    [InlineData("/Sub/App/PathAndPathBase", "/Sub/App/PathAndPathBase", "")]
    [InlineData("/SUb/APp/PathAndPAthBase", "/SUb/APp/PathAndPAthBase", "")]
    [InlineData(@"/Sub\App/PathAndPathBase/", @"/Sub\App/PathAndPathBase", "/")]
    [InlineData("/Sub%2FApp/PathAndPathBase/", "/Sub%2FApp/PathAndPathBase", "/")]
    [InlineData("/Sub%2fApp/PathAndPathBase/", "/Sub%2fApp/PathAndPathBase", "/")]
    [InlineData("/Sub%5cApp/PathAndPathBase/", @"/Sub\App/PathAndPathBase", "/")]
    [InlineData("/Sub%5CApp/PathAndPathBase/", @"/Sub\App/PathAndPathBase", "/")]
    [InlineData("/Sub/App/PathAndPathBase/Path", "/Sub/App/PathAndPathBase", "/Path")]
    [InlineData("/Sub/App/PathANDPathBase/PATH", "/Sub/App/PathANDPathBase", "/PATH")]
    public async Task RequestPathBase_Split(string url, string expectedPathBase, string expectedPath)
    {
        // The test app trims the test name off of the request path and puts it on the PathBase.
        // /AppName/TestName/Path
        var (status, body) = await SendSocketRequestAsync(url);
        Assert.Equal(200, status);
        Assert.Equal($"PathBase: {expectedPathBase}; Path: {expectedPath}", body);
    }

    [ConditionalTheory]
    [SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;" + "Windows.Amd64.VS2022.Pre;")]
    [RequiresNewHandler]
    [InlineData("//Sub/App/PathAndPathBase", "//Sub/App/PathAndPathBase", "")]
    [InlineData(@"/\Sub/App/PathAndPathBase/", @"/\Sub/App/PathAndPathBase", "/")]
    [InlineData(@"/Sub/\App/PathAndPathBase//path", @"/Sub/\App/PathAndPathBase", "//path")]
    [InlineData("/%2FSub/App/PathAndPathBase/", "/%2FSub/App/PathAndPathBase", "/")]
    [InlineData("/%5CSub/App/PathAndPathBase/", @"/\Sub/App/PathAndPathBase", "/")]
    [InlineData("///Sub/App/PathAndPathBase/path1/path2", "///Sub/App/PathAndPathBase", "/path1/path2")]
    [InlineData("/Sub%2F/App/PathAndPathBase/%2FPath", "/Sub%2F/App/PathAndPathBase", "/%2FPath")]
    [InlineData(@"/%2F\/Sub/App/PathAndPathBase/Path", @"/%2F\/Sub/App/PathAndPathBase", "/Path")]
    [InlineData(@"/Sub/App/PathANDPathBase/PATH", @"/Sub/App/PathANDPathBase", "/PATH")]
    [InlineData("/Sub/%5cApp/PathAndPathBase/", @"/Sub/\App/PathAndPathBase", "/")]
    [InlineData("//Sub//App/PathAndPathBase//Path", "//Sub//App/PathAndPathBase", "//Path")]
    [InlineData(@"/Sub/ball/../App/PathAndPathBase/path1//path2", @"/Sub/App/PathAndPathBase", "/path1//path2")]
    [InlineData(@"/Sub//ball/../App/PathAndPathBase/path1//path2", @"/Sub//App/PathAndPathBase", "/path1//path2")]
    // The results should be "/Sub//App/PathAndPathBase", "//path1//path2", but Http.Sys collapses the "//" before the "../"
    // and we don't have a good way of emulating that.
    // [InlineData(@"/Sub/call//../App/PathAndPathBase//path1//path2", @"", "/Sub/call/App/PathAndPathBase//path1//path2")]
    [InlineData(@"/Sub/call/.%2e/App/PathAndPathBase//path1//path2", @"/Sub/App/PathAndPathBase", "//path1//path2")]
    [InlineData(@"/Sub/call/.%2E/App/PathAndPathBase//path1//path2", @"/Sub/App/PathAndPathBase", "//path1//path2")]
    public async Task RequestPathBase_WithDoubleSlashes_Split(string url, string expectedPathBase, string expectedPath)
    {
        // The test app trims the test name off of the request path and puts it on the PathBase.
        // /AppName/TestName/Path
        var (status, body) = await SendSocketRequestAsync(url);
        Assert.Equal(200, status);
        Assert.Equal($"PathBase: {expectedPathBase}; Path: {expectedPath}", body);
    }

    private async Task<(int Status, string Body)> SendSocketRequestAsync(string path)
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            await connection.Send(
                "GET " + path + " HTTP/1.1",
                "Host: " + _fixture.Client.BaseAddress.Authority,
                "",
                "");
            var headers = await connection.ReceiveHeaders();
            var status = int.Parse(headers[0].Substring(9, 3), CultureInfo.InvariantCulture);
            if (headers.Contains("Transfer-Encoding: chunked"))
            {
                var bytes0 = await connection.ReceiveChunk();
                return (status, Encoding.UTF8.GetString(bytes0.Span));
            }
            var length = int.Parse(headers.Single(h => h.StartsWith("Content-Length: ", StringComparison.Ordinal))["Content-Length: ".Length..], CultureInfo.InvariantCulture);
            var bytes1 = await connection.Receive(length);
            return (status, Encoding.ASCII.GetString(bytes1.Span));
        }
    }
}
