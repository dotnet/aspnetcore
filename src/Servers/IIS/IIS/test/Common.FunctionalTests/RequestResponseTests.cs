// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

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

[Collection(IISTestSiteCollectionInProc.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class RequestResponseTests
{
    private readonly IISTestSiteFixture _fixture;

    public RequestResponseTests(IISTestSiteFixture fixture)
    {
        _fixture = fixture;
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task RequestPath_UrlUnescaping()
    {
        // Must start with '/'
        var stringBuilder = new StringBuilder("/RequestPath/");
        for (var i = 32; i < 127; i++)
        {
            if (i == 43)
            {
                continue; // %2B "+" gives a 404.11 (URL_DOUBLE_ESCAPED)
            }
            stringBuilder.Append("%");
            stringBuilder.Append(i.ToString("X2", CultureInfo.InvariantCulture));
        }
        var rawPath = stringBuilder.ToString();
        var response = await SendSocketRequestAsync(rawPath);
        Assert.Equal(200, response.Status);
        // '/' %2F is an exception, un-escaping it would change the structure of the path
        Assert.Equal("/ !\"#$%&'()*,-.%2F0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", response.Body);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Request_WithDoubleSlashes_LeftAlone()
    {
        var rawPath = "/RequestPath//a/b//c";
        var response = await SendSocketRequestAsync(rawPath);
        Assert.Equal(200, response.Status);
        Assert.Equal("//a/b//c", response.Body);
    }

    [ConditionalTheory]
    [RequiresNewHandler]
    [InlineData("/RequestPath/a/b/../c", "/a/c")]
    [InlineData("/RequestPath/a/b/./c", "/a/b/c")]
    public async Task Request_WithNavigation_Removed(string input, string expectedPath)
    {
        var response = await SendSocketRequestAsync(input);
        Assert.Equal(200, response.Status);
        Assert.Equal(expectedPath, response.Body);
    }

    [ConditionalTheory]
    [RequiresNewHandler]
    [InlineData("/RequestPath/a/b/%2E%2E/c", "/a/c")]
    [InlineData("/RequestPath/a/b/%2E/c", "/a/b/c")]
    public async Task Request_WithEscapedNavigation_Removed(string input, string expectedPath)
    {
        var response = await SendSocketRequestAsync(input);
        Assert.Equal(200, response.Status);
        Assert.Equal(expectedPath, response.Body);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Request_ControlCharacters_400()
    {
        for (var i = 0; i < 32; i++)
        {
            if (i == 9 || i == 10)
            {
                continue; // \t and \r are allowed by Http.Sys.
            }
            var response = await SendSocketRequestAsync("/" + (char)i);
            Assert.True(string.Equals(400, response.Status), i.ToString("X2", CultureInfo.InvariantCulture) + ";" + response);
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Request_EscapedControlCharacters_400()
    {
        for (var i = 0; i < 32; i++)
        {
            var response = await SendSocketRequestAsync("/%" + i.ToString("X2", CultureInfo.InvariantCulture));
            Assert.True(string.Equals(400, response.Status), i.ToString("X2", CultureInfo.InvariantCulture) + ";" + response);
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task PassesThroughCompressionOutOfProcess()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/CompressedData");

        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await _fixture.Client.SendAsync(request);
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
        Assert.Equal(
            new byte[] {
                0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x04, 0x0A, 0x63, 0xA0, 0x03, 0x00, 0x00,
                0xCA, 0xC6, 0x88, 0x99, 0x64, 0x00, 0x00, 0x00 },
            await response.Content.ReadAsByteArrayAsync());
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task PassesThroughCompressionInProcess()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/CompressedData");

        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        var response = await _fixture.Client.SendAsync(request);
        Assert.Equal("gzip", response.Content.Headers.ContentEncoding.Single());
        Assert.Equal(
            new byte[] {
                0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x04, 0x0A, 0x63, 0xA0, 0x03, 0x00, 0x00,
                0xCA, 0xC6, 0x88, 0x99, 0x64, 0x00, 0x00, 0x00 },
            await response.Content.ReadAsByteArrayAsync());
    }

    [ConditionalFact]
    public async Task ReadAndWriteSynchronously()
    {
        var content = new StringContent(new string('a', 100000));
        for (int i = 0; i < 50; i++)
        {
            var response = await _fixture.Client.PostAsync("ReadAndWriteSynchronously", content);
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(expected: 110000, actual: responseText.Length);
        }
    }

    [ConditionalFact]
    public async Task ReadAndWriteEcho()
    {
        var body = new string('a', 100000);
        var content = new StringContent(body);
        var response = await _fixture.Client.PostAsync("ReadAndWriteEcho", content);
        var responseText = await response.Content.ReadAsStringAsync();

        Assert.Equal(body, responseText);
    }

    [ConditionalFact]
    public async Task ReadAndWriteCopyToAsync()
    {
        var body = new string('a', 100000);
        var content = new StringContent(body);
        var response = await _fixture.Client.PostAsync("ReadAndWriteCopyToAsync", content);
        var responseText = await response.Content.ReadAsStringAsync();

        Assert.Equal(body, responseText);
    }

    [ConditionalFact]
    public async Task ReadAndWriteEchoTwice()
    {
        var requestBody = new string('a', 10000);
        var content = new StringContent(requestBody);
        var response = await _fixture.Client.PostAsync("ReadAndWriteEchoTwice", content);
        var responseText = await response.Content.ReadAsStringAsync();

        Assert.Equal(requestBody.Length * 2, responseText.Length);
    }

    [ConditionalFact]
    public async Task ReadSetHeaderWrite()
    {
        var body = "Body text";
        var content = new StringContent(body);
        var response = await _fixture.Client.PostAsync("SetHeaderFromBody", content);
        var responseText = await response.Content.ReadAsStringAsync();

        Assert.Equal(body, response.Headers.GetValues("BodyAsString").Single());
        Assert.Equal(body, responseText);
    }

    [ConditionalFact]
    public async Task ReadAndWriteSlowConnection()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            var testString = "hello world";
            var request = $"POST /ReadAndWriteSlowConnection HTTP/1.0\r\n" +
                $"Content-Length: {testString.Length}\r\n" +
                "Host: " + "localhost\r\n" +
                "\r\n" + testString;

            foreach (var c in request)
            {
                await connection.Send(c.ToString());
                await Task.Delay(10);
            }

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
            await connection.ReceiveHeaders();

            for (int i = 0; i < 100; i++)
            {
                foreach (var c in testString)
                {
                    await connection.Receive(c.ToString());
                }
                await Task.Delay(10);
            }
            await connection.WaitForConnectionClose();
        }
    }

    [ConditionalFact]
    public async Task ReadAndWriteInterleaved()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            var requestLength = 0;
            var messages = new List<string>();
            for (var i = 1; i < 100; i++)
            {
                var message = i + Environment.NewLine;
                requestLength += message.Length;
                messages.Add(message);
            }

            await connection.Send(
                "POST /ReadAndWriteEchoLines HTTP/1.0",
                $"Content-Length: {requestLength}",
                "Host: localhost",
                "",
                "");

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
            await connection.ReceiveHeaders();

            foreach (var message in messages)
            {
                await connection.Send(message);
                await connection.Receive(message);
            }

            await connection.Send("\r\n");
            await connection.WaitForConnectionClose();
        }
    }

    [ConditionalFact]
    public async Task ConsumePartialBody()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            var message = "Hello";
            await connection.Send(
                "POST /ReadPartialBody HTTP/1.1",
                $"Content-Length: {100}",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await connection.Send(message);

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");

            // This test can return both content length or chunked response
            // depending on if appfunc managed to complete before write was
            // issued
            var headers = await connection.ReceiveHeaders();
            if (headers.Contains("Content-Length: 5"))
            {
                await connection.Receive("Hello");
            }
            else
            {
                await connection.Receive(
                    "5",
                    message,
                    "");
                await connection.Receive(
                    "0",
                    "",
                    "");
            }

            await connection.WaitForConnectionClose();
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task AsyncChunkedPostIsAccepted()
    {
        // This test sends a lot of request because we are trying to force
        // different async completion modes from IIS
        for (int i = 0; i < 100; i++)
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                await connection.Send(
                    "POST /ReadFullBody HTTP/1.1",
                    $"Transfer-Encoding: chunked",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                await connection.Send("5",
                    "Hello",
                    "");

                await connection.Send(
                    "0",
                    "",
                    "");

                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "");

                await connection.ReceiveHeaders();
                await connection.Receive("Completed");

                await connection.WaitForConnectionClose();
            }
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ResponseBodyTest_UnflushedPipe_AutoFlushed()
    {
        Assert.Equal(10, (await _fixture.Client.GetByteArrayAsync($"/UnflushedResponsePipe")).Length);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ResponseBodyTest_FlushedPipeAndThenUnflushedPipe_AutoFlushed()
    {
        Assert.Equal(20, (await _fixture.Client.GetByteArrayAsync($"/FlushedPipeAndThenUnflushedPipe")).Length);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ResponseBodyTest_GettingHttpContextFieldsWork()
    {
        Assert.Equal("SlowOnCompleted", await _fixture.Client.GetStringAsync($"/OnCompletedHttpContext"));
        Assert.Equal("", await _fixture.Client.GetStringAsync($"/OnCompletedHttpContext_Completed"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task ResponseBodyTest_CompleteAsyncWorks()
    {
        // The app func for CompleteAsync will not finish until CompleteAsync_Completed is sent.
        // This verifies that the response is sent to the client with CompleteAsync
        var response = await _fixture.Client.GetAsync("/CompleteAsync");
        Assert.True(response.IsSuccessStatusCode);

        var response2 = await _fixture.Client.GetAsync("/CompleteAsync_Completed");
        Assert.True(response2.IsSuccessStatusCode);
    }

    [ConditionalFact]
    public async Task ProvidesAccessToServerVariables()
    {
        var port = _fixture.Client.BaseAddress.Port;
        Assert.Equal("SERVER_PORT: " + port, await _fixture.Client.GetStringAsync("/ServerVariable?q=SERVER_PORT"));
        Assert.Equal("QUERY_STRING: q=QUERY_STRING", await _fixture.Client.GetStringAsync("/ServerVariable?q=QUERY_STRING"));
    }

    [ConditionalFact]
    public async Task ReturnsNullForUndefinedServerVariable()
    {
        Assert.Equal("THIS_VAR_IS_UNDEFINED: (null)", await _fixture.Client.GetStringAsync("/ServerVariable?q=THIS_VAR_IS_UNDEFINED"));
    }

    [ConditionalFact]
    public async Task CanSetAndReadVariable()
    {
        Assert.Equal("ROUNDTRIP: 1", await _fixture.Client.GetStringAsync("/ServerVariable?v=1&q=ROUNDTRIP"));
    }

    [ConditionalFact]
    public async Task BasePathIsNotPrefixedBySlashSlashQuestionMark()
    {
        Assert.DoesNotContain(@"\\?\", await _fixture.Client.GetStringAsync("/BasePath"));
    }

    [ConditionalFact]
    public async Task GetServerVariableDoesNotCrash()
    {
        await Helpers.StressLoad(_fixture.Client, "/GetServerVariableStress", response =>
        {
            var text = response.Content.ReadAsStringAsync().Result;
            Assert.StartsWith("Response Begin", text);
            Assert.EndsWith("Response End", text);
        });
    }

    [ConditionalFact]
    public async Task TestStringValuesEmptyForMissingHeaders()
    {
        var result = await _fixture.Client.GetStringAsync($"/TestRequestHeaders");
        Assert.Equal("Success", result);
    }

    [ConditionalFact]
    public async Task TestReadOffsetWorks()
    {
        var result = await _fixture.Client.PostAsync($"/TestReadOffsetWorks", new StringContent("Hello World"));
        Assert.Equal("Hello World", await result.Content.ReadAsStringAsync());
    }

    [ConditionalTheory]
    [InlineData("/InvalidOffsetSmall")]
    [InlineData("/InvalidOffsetLarge")]
    [InlineData("/InvalidCountSmall")]
    [InlineData("/InvalidCountLarge")]
    [InlineData("/InvalidCountWithOffset")]
    public async Task TestInvalidReadOperations(string operation)
    {
        var result = await _fixture.Client.GetStringAsync($"/TestInvalidReadOperations{operation}");
        Assert.Equal("Success", result);
    }

    [ConditionalTheory]
    [InlineData("/NullBuffer")]
    [InlineData("/InvalidCountZeroRead")]
    public async Task TestValidReadOperations(string operation)
    {
        var result = await _fixture.Client.GetStringAsync($"/TestValidReadOperations{operation}");
        Assert.Equal("Success", result);
    }

    [ConditionalTheory]
    [InlineData("/NullBufferPost")]
    [InlineData("/InvalidCountZeroReadPost")]
    public async Task TestValidReadOperationsPost(string operation)
    {
        var result = await _fixture.Client.PostAsync($"/TestValidReadOperations{operation}", new StringContent("hello"));
        Assert.Equal("Success", await result.Content.ReadAsStringAsync());
    }

    [ConditionalTheory]
    [InlineData("/InvalidOffsetSmall")]
    [InlineData("/InvalidOffsetLarge")]
    [InlineData("/InvalidCountSmall")]
    [InlineData("/InvalidCountLarge")]
    [InlineData("/InvalidCountWithOffset")]
    public async Task TestInvalidWriteOperations(string operation)
    {
        var result = await _fixture.Client.GetStringAsync($"/TestInvalidWriteOperations{operation}");
        Assert.Equal("Success", result);
    }

    [ConditionalFact]
    public async Task TestValidWriteOperations()
    {
        var result = await _fixture.Client.GetStringAsync($"/TestValidWriteOperations/NullBuffer");
        Assert.Equal("Success", result);
    }

    [ConditionalFact]
    public async Task TestValidWriteOperationsPost()
    {
        var result = await _fixture.Client.PostAsync($"/TestValidWriteOperations/NullBufferPost", new StringContent("hello"));
        Assert.Equal("Success", await result.Content.ReadAsStringAsync());
    }

    [ConditionalFact]
    public async Task AddEmptyHeaderSkipped()
    {
        var response = await _fixture.Client.GetAsync("ResponseEmptyHeaders");
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("EmptyHeader", out var headerValues));
    }

    [ConditionalFact]
    public async Task AddResponseHeaders_HeaderValuesAreSetCorrectly()
    {
        var response = await _fixture.Client.GetAsync("ResponseHeaders");
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Request Complete", responseText);

        Assert.True(response.Headers.TryGetValues("UnknownHeader", out var headerValues));
        Assert.Equal("test123=foo", headerValues.First());

        Assert.True(response.Content.Headers.TryGetValues(Net.Http.Headers.HeaderNames.ContentType, out headerValues));
        Assert.Equal("text/plain", headerValues.First());

        Assert.True(response.Headers.TryGetValues("MultiHeader", out headerValues));
        Assert.Equal(2, headerValues.Count());
        Assert.Equal("1", headerValues.First());
        Assert.Equal("2", headerValues.Last());
    }

    [ConditionalFact]
    public async Task ErrorCodeIsSetForExceptionDuringRequest()
    {
        var response = await _fixture.Client.GetAsync("Throw");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Internal Server Error", response.ReasonPhrase);
    }

    [ConditionalTheory]
    [InlineData(200, "custom", "custom", null)]
    [InlineData(200, "custom", "custom", "Custom body")]
    [InlineData(200, "custom", "custom", "")]

    [InlineData(500, "", "Internal Server Error", null)]
    [InlineData(500, "", "Internal Server Error", "Custom body")]
    [InlineData(500, "", "Internal Server Error", "")]

    [InlineData(400, "custom", "custom", null)]
    [InlineData(400, "", "Bad Request", "Custom body")]
    [InlineData(400, "", "Bad Request", "")]

    [InlineData(999, "", "", null)]
    [InlineData(999, "", "", "Custom body")]
    [InlineData(999, "", "", "")]
    public async Task CustomErrorCodeWorks(int code, string reason, string expectedReason, string body)
    {
        var response = await _fixture.Client.GetAsync($"SetCustomErorCode?code={code}&reason={reason}&writeBody={body != null}&body={body}");
        Assert.Equal((HttpStatusCode)code, response.StatusCode);
        Assert.Equal(expectedReason, response.ReasonPhrase);

        // ReadAsStringAsync returns empty string for empty results
        Assert.Equal(body ?? string.Empty, await response.Content.ReadAsStringAsync());
    }

    [ConditionalTheory]
    [RequiresNewHandler]
    [InlineData(204, "GET")]
    [InlineData(304, "GET")]
    public async Task TransferEncodingNotSetForStatusCodes(int code, string method)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), _fixture.Client.BaseAddress + $"SetCustomErorCode?code={code}");
        var response = await _fixture.Client.SendAsync(request);
        Assert.Equal((HttpStatusCode)code, response.StatusCode);
        Assert.DoesNotContain(response.Headers, h => h.Key.Equals("transfer-encoding", StringComparison.InvariantCultureIgnoreCase));
    }

    [ConditionalFact]
    public async Task ServerHeaderIsOverriden()
    {
        var response = await _fixture.Client.GetAsync("OverrideServer");
        Assert.Equal("MyServer/7.8", response.Headers.Server.Single().Product.ToString());
    }

    [ConditionalTheory]
    [InlineData("SetStatusCodeAfterWrite")]
    [InlineData("SetHeaderAfterWrite")]
    public async Task ResponseInvalidOrderingTests_ExpectFailure(string path)
    {
        Assert.Equal($"Started_{path}Threw_Finished", await _fixture.Client.GetStringAsync("/ResponseInvalidOrdering/" + path));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task HostingEnvironmentIsCorrect()
    {
        Assert.Equal(_fixture.DeploymentResult.ContentRoot, await _fixture.Client.GetStringAsync("/ContentRootPath"));
        Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\wwwroot", await _fixture.Client.GetStringAsync("/WebRootPath"));
        Assert.Equal(_fixture.DeploymentResult.ContentRoot, await _fixture.DeploymentResult.HttpClient.GetStringAsync("/CurrentDirectory"));
        Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\", await _fixture.Client.GetStringAsync("/BaseDirectory"));
        Assert.Equal(_fixture.DeploymentResult.ContentRoot + "\\", await _fixture.Client.GetStringAsync("/ASPNETCORE_IIS_PHYSICAL_PATH"));
    }

    [ConditionalTheory]
    [InlineData("IIISEnvironmentFeature")]
    [InlineData("IIISEnvironmentFeatureConfig")]
    public async Task IISEnvironmentFeatureIsAvailable(string endpoint)
    {
        var siteName = _fixture.DeploymentResult.DeploymentParameters.SiteName.ToUpperInvariant();
    
        var expected = $"""
            IIS Version: 10.0
            ApplicationId: /LM/W3SVC/1/ROOT
            Application Path: {_fixture.DeploymentResult.ContentRoot}\
            Application Virtual Path: /
            Application Config Path: MACHINE/WEBROOT/APPHOST/{siteName}
            AppPool ID: {_fixture.DeploymentResult.AppPoolName}
            AppPool Config File: {_fixture.DeploymentResult.DeploymentParameters.ServerConfigLocation}
            Site ID: 1
            Site Name: {siteName}
            """;

        Assert.Equal(expected, await _fixture.Client.GetStringAsync($"/{endpoint}"));
    }

    [ConditionalTheory]
    [InlineData(65000)]
    [InlineData(1000000)]
    [InlineData(10000000)]
    [InlineData(100000000)]
    public async Task LargeResponseBodyTest_CheckAllResponseBodyBytesWritten(int query)
    {
        Assert.Equal(new string('a', query), await _fixture.Client.GetStringAsync($"/LargeResponseBody?length={query}"));
    }

    [ConditionalFact]
    public async Task LargeResponseBodyFromFile_CheckAllResponseBodyBytesWritten()
    {
        Assert.Equal(200000000, (await _fixture.Client.GetStringAsync($"/LargeResponseFile")).Length);
    }

    [ConditionalTheory]
    [InlineData("FeatureCollectionSetRequestFeatures")]
    [InlineData("FeatureCollectionSetResponseFeatures")]
    [InlineData("FeatureCollectionSetConnectionFeatures")]
    public async Task FeatureCollectionTest_SetHttpContextFeatures(string path)
    {
        Assert.Equal("Success", await _fixture.Client.GetStringAsync(path + "/path" + "?query"));
    }

    [ConditionalFact]
    [RequiresNewHandler]
    [RequiresNewShim]
    public async Task ExposesIServerAddressesFeature()
    {
        Assert.Equal(_fixture.Client.BaseAddress.ToString(), await _fixture.Client.GetStringAsync("/ServerAddresses"));
    }

    [ConditionalFact]
    public async Task ServerWorksAfterClientDisconnect()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            var message = "Hello";
            await connection.Send(
                "POST /ReadAndWriteSynchronously HTTP/1.1",
                $"Content-Length: {100000}",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await connection.Send(message);

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
        }

        var response = await _fixture.Client.GetAsync("HelloWorld");

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World", responseText);
    }

    [ConditionalFact]
    public async Task RequestAbortedTokenFires()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            await connection.Send(
                "GET /WaitForAbort HTTP/1.1",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "1");
        }

        await _fixture.Client.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "0");
    }

    [ConditionalFact]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2, SkipReason = "Shutdown hangs https://github.com/dotnet/aspnetcore/issues/25107")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81, SkipReason = "NullReferenceException https://github.com/dotnet/aspnetcore/issues/26839")]
    public async Task ClientDisconnectStress()
    {
        var maxRequestSize = 1000;
        var blockSize = 40;
        async Task RunRequests()
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                await connection.Send(
                    "POST /ReadAndFlushEcho HTTP/1.1",
                    $"Content-Length: {maxRequestSize}",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");

                var disconnectAfter = Random.Shared.Next(maxRequestSize);
                var data = new byte[blockSize];
                for (int i = 0; i < disconnectAfter / blockSize; i++)
                {
                    await connection.Stream.WriteAsync(data);
                }
            }
        }

        List<Task> tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(RunRequests));
        }

        await Task.WhenAll(tasks);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task SendTransferEncodingHeadersWithMultipleValues()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            await connection.Send(
                "POST /TransferEncodingHeadersWithMultipleValues HTTP/1.1",
                "Transfer-Encoding: gzip, chunked",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
        }
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task SendTransferEncodingAndContentLength_ContentLengthShouldBeRemoved()
    {
        using (var connection = _fixture.CreateTestConnection())
        {
            await connection.Send(
                "POST /TransferEncodingAndContentLengthShouldBeRemove HTTP/1.1",
                "Transfer-Encoding: gzip, chunked",
                "Content-Length: 5",
                "Host: localhost",
                "Connection: close",
                "",
                "");

            await connection.Receive(
                "HTTP/1.1 200 OK",
                "");
        }
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
                Assert.False(bytes0.IsEmpty);
                return (status, Encoding.UTF8.GetString(bytes0.Span));
            }
            var length = int.Parse(headers.Single(h => h.StartsWith("Content-Length: ", StringComparison.Ordinal)).Substring("Content-Length: ".Length), CultureInfo.InvariantCulture);
            var bytes1 = await connection.Receive(length);
            return (status, Encoding.ASCII.GetString(bytes1.Span));
        }
    }
}
