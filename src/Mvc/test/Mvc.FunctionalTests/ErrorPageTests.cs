// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Functional test to verify the error reporting of Razor compilation by diagnostic middleware.
/// </summary>
public class ErrorPageTests : LoggedTest
{
    private static readonly string PreserveCompilationContextMessage = HtmlEncoder.Default.Encode(
        "One or more compilation references may be missing. " +
        "If you're seeing this in a published application, set 'CopyRefAssembliesToPublishDirectory' to true in your project file to ensure files in the refs directory are published.");

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<ErrorPageMiddlewareWebSite.Startup>(LoggerFactory)
            .WithWebHostBuilder(b => b.UseStartup<ErrorPageMiddlewareWebSite.Startup>());
        Client = Factory.CreateDefaultClient();
        // These tests want to verify runtime compilation and formatting in the HTML of the error page
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<ErrorPageMiddlewareWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task CompilationFailuresAreListedByErrorPageMiddleware()
    {
        // Arrange
        var factory = Factory.WithWebHostBuilder(b => b.UseStartup<ErrorPageMiddlewareWebSite.Startup>());
        factory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(serviceCollection => serviceCollection.Configure<MvcRazorRuntimeCompilationOptions>(ConfigureRuntimeCompilationOptions)));

        var client = factory.CreateDefaultClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var action = "CompilationFailure";
        var expected = "Cannot implicitly convert type &#x27;int&#x27; to &#x27;string&#x27;";
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await client.GetAsync("http://localhost/" + action);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"{action}.cshtml", content);
        Assert.Contains(expected, content);
        Assert.DoesNotContain(PreserveCompilationContextMessage, content);

        static void ConfigureRuntimeCompilationOptions(MvcRazorRuntimeCompilationOptions options)
        {
            options.AdditionalReferencePaths.Add(typeof(string).Assembly.Location);

            // Workaround for incorrectly generated deps file. The build output has all of the binaries required to compile. We'll grab these and
            // add it to the list of assemblies runtime compilation uses.
            foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            {
                options.AdditionalReferencePaths.Add(path);
            }
        }
    }

    [Fact]
    public async Task ParseFailuresAreListedByErrorPageMiddleware()
    {
        // Arrange
        var action = "ParserError";
        var expected = "The code block is missing a closing &quot;}&quot; character.  Make sure you " +
        "have a matching &quot;}&quot; character for all the &quot;{&quot; characters " +
        "within this block, and that none of the &quot;}&quot; characters are being " +
        "interpreted as markup.";
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await Client.GetAsync(action);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"{action}.cshtml", content);
        Assert.Contains(expected, content);
    }

    [Fact]
    public async Task CompilationFailuresFromViewImportsAreListed()
    {
        // Arrange
        var expectedMessage = "The type or namespace name &#x27;NamespaceDoesNotExist&#x27; could not be found ("
            + "are you missing a using directive or an assembly reference?)";
        var expectedCompilationContent = "Views_ErrorFromViewImports_Index : "
            + "global::Microsoft.AspNetCore.Mvc.Razor.RazorPage&lt;dynamic&gt;";
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await Client.GetAsync("http://localhost/ErrorFromViewImports");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("_ViewImports.cshtml", content);
        Assert.Contains(expectedMessage, content);
        Assert.Contains(PreserveCompilationContextMessage, content);
        Assert.Contains(expectedCompilationContent, content);
    }

    [Fact]
    public async Task RuntimeErrorAreListedByErrorPageMiddleware()
    {
        // Arrange
        var expectedMessage = HtmlEncoder.Default.Encode("throw new Exception(\"Error from view\");");
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await Client.GetAsync("http://localhost/RuntimeError");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("RuntimeError.cshtml", content);
        Assert.Contains(expectedMessage, content);
    }

    [Fact]
    public async Task LoaderExceptionsFromReflectionTypeLoadExceptionsAreListed()
    {
        // Arrange
        var expectedMessage = "Custom Loader Exception.";
        var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

        // Act
        var response = await Client.GetAsync("http://localhost/LoaderException");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Loader Exceptions:", content);
        Assert.Contains(expectedMessage, content);
    }

    [Fact]
    public async Task AggregateException_FlattensInnerExceptions()
    {
        // Arrange
        var aggregateException = "AggregateException: One or more errors occurred.";
        var nullReferenceException = "NullReferenceException: Foo cannot be null";
        var indexOutOfRangeException = "IndexOutOfRangeException: Index is out of range";

        // Act
        var response = await Client.GetAsync("http://localhost/AggregateException");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains(aggregateException, content);
        Assert.Contains(nullReferenceException, content);
        Assert.Contains(indexOutOfRangeException, content);
    }
}
