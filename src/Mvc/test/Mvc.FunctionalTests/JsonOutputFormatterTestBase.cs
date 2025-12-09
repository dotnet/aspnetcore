// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using FormatterWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class JsonOutputFormatterTestBase<TStartup> : LoggedTest where TStartup : class
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

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

    [Fact]
    public virtual async Task SerializableErrorIsReturnedInExpectedFormat()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Employee xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
            "<Id>2</Id><Name>foo</Name></Employee>";

        var expectedOutput = "{\"Id\":[\"The field Id must be between 10 and 100." +
                "\"],\"Name\":[\"The field Name must be a string or array type with" +
                " a minimum length of '15'.\"]}";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/CreateEmployee");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedOutput, actualContent);

        var modelStateErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(actualContent);
        Assert.Equal(2, modelStateErrors.Count);

        var errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Id").Value;

        var error = Assert.Single(errors);
        Assert.Equal("The field Id must be between 10 and 100.", error);

        errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Name").Value;
        error = Assert.Single(errors);
        Assert.Equal("The field Name must be a string or array type with a minimum length of '15'.", error);
    }

    [Fact]
    public virtual async Task Formatting_IntValue()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.IntResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("2", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_StringValue()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("\"Hello world\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_StringValueWithUnicodeContent()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithUnicodeResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("\"Hello Mr. 🦊\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_StringValueWithNonAsciiCharacters()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithNonAsciiContent)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("\"Une bête de cirque\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_SimpleModel()
    {
        // Arrange
        var expected = "{\"id\":10,\"name\":\"Test\",\"streetName\":\"Some street\"}";

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.SimpleModelResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_CollectionType()
    {
        // Arrange
        var expected = "[{\"id\":10,\"name\":\"TestName\",\"streetName\":null},{\"id\":11,\"name\":\"TestName1\",\"streetName\":\"Some street\"}]";

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.CollectionModelResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_DictionaryType()
    {
        // Arrange
        var expected = "{\"SomeKey\":\"Value0\",\"DifferentKey\":\"Value1\",\"Key3\":null}";

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.DictionaryResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_LargeObject()
    {
        // Arrange
        var expectedName = "This is long so we can test large objects " + new string('a', 1024 * 65);
        var expected = $"{{\"id\":10,\"name\":\"{expectedName}\",\"streetName\":null}}";

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.LargeObjectResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public virtual async Task Formatting_ProblemDetails()
    {
        using var _ = new ActivityReplacer();

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.ProblemDetailsResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);

        var obj = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", obj.Value<string>("type"));
        Assert.Equal("Not Found", obj.Value<string>("title"));
        Assert.Equal("404", obj.Value<string>("status"));
        Assert.NotNull(obj.Value<string>("traceId"));
    }

    [Fact]
    public virtual async Task Formatting_PolymorphicModel()
    {
        // Arrange
        var expected = "{\"address\":\"Some address\",\"id\":10,\"name\":\"test\",\"streetName\":null}";

        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.PolymorphicResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }
}
