// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class TempDataTestBase<TStartup> : LoggedTest where TStartup : class
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<TStartup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<TStartup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    protected virtual void ConfigureWebHostBuilder(IWebHostBuilder builder) { }

    [Fact]
    public async Task PersistsJustForNextRequest()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.PostAsync("/TempData/SetTempData", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);

        // Act 3
        response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 3
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ViewRendersTempData()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act
        var response = await Client.PostAsync("/TempData/DisplayTempData", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);
    }

    [Fact]
    public async Task Redirect_RetainsTempData_EvenIfAccessed()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.PostAsync("/TempData/SetTempData", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        var redirectResponse = await Client.SendAsync(GetRequest("/TempData/GetTempDataAndRedirect", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

        // Act 3
        response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), response));

        // Assert 3
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);
    }

    [Fact]
    public async Task Peek_RetainsTempData()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.PostAsync("/TempData/SetTempData", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        var peekResponse = await Client.SendAsync(GetRequest("/TempData/PeekTempData", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, peekResponse.StatusCode);
        var body = await peekResponse.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);

        // Act 3
        var getResponse = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 3
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        body = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);
    }

    [Fact]
    public async Task ValidTypes_RoundTripProperly()
    {
        // Arrange
        var testGuid = Guid.NewGuid();
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
                new KeyValuePair<string, string>("intValue", "10"),
                new KeyValuePair<string, string>("listValues", "Foo1"),
                new KeyValuePair<string, string>("listValues", "Foo2"),
                new KeyValuePair<string, string>("listValues", "Foo3"),
                new KeyValuePair<string, string>("datetimeValue", "10/10/2010"),
                new KeyValuePair<string, string>("guidValue", testGuid.ToString()),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var redirectResponse = await Client.PostAsync("/TempData/SetTempDataMultiple", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

        // Act 2
        var response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), redirectResponse));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal($"Foo 10 3 10/10/2010 00:00:00 {testGuid.ToString()}", body);
    }

    [Fact]
    public async Task ResponseWrite_DoesNotCrashSaveTempDataFilter()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Name", "Jordan"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act, checking it didn't throw
        var response = await Client.GetAsync("/TempData/SetTempDataResponseWrite");
    }

    [Fact]
    public async Task SetInActionResultExecution_AvailableForNextRequest()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Name", "Jordan"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.GetAsync("/TempData/SetTempDataInActionResult");

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        response = await Client.SendAsync(GetRequest("/TempData/GetTempDataSetInActionResult", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Michael", body);

        // Act 3
        response = await Client.SendAsync(GetRequest("/TempData/GetTempDataSetInActionResult", response));

        // Assert 3
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SaveTempDataFilter_DoesNotSaveTempData_OnUnhandledException()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/TempData/UnhandledExceptionAndSettingTempData");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Exception from action UnhandledExceptionAndSettingTempData", responseBody);

        // Arrange & Act
        response = await Client.GetAsync("/TempData/UnhandledExceptionAndGetTempData");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SaveTempDataFilter_DoesNotSaveTempData_OnHandledExceptions()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/TempData/UnhandledExceptionAndSettingTempData?handleException=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("Exception was handled in TestExceptionFilter", responseBody);

        // Arrange & Act
        response = await Client.GetAsync("/TempData/UnhandledExceptionAndGetTempData");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    public HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        IEnumerable<string> values;
        if (response.Headers.TryGetValues("Set-Cookie", out values))
        {
            foreach (var cookie in SetCookieHeaderValue.ParseList(values.ToList()))
            {
                if (cookie.Expires == null || cookie.Expires >= DateTimeOffset.UtcNow)
                {
                    request.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                }
            }
        }
        return request;
    }
}
