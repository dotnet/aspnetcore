// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// Functional test to verify the error reporting of Razor compilation by diagnostic middleware.
    /// </summary>
    public class ErrorPageTests : IClassFixture<MvcTestFixture<ErrorPageMiddlewareWebSite.Startup>>, IDisposable
    {
        private static readonly string PreserveCompilationContextMessage = HtmlEncoder.Default.Encode(
            "One or more compilation references may be missing. " +
            "If you're seeing this in a published application, set 'CopyRefAssembliesToPublishDirectory' to true in your project file to ensure files in the refs directory are published.");
        private readonly AssemblyTestLog _assemblyTestLog;

        private readonly MvcTestFixture<ErrorPageMiddlewareWebSite.Startup> _fixture;

        public ErrorPageTests(
            MvcTestFixture<ErrorPageMiddlewareWebSite.Startup> fixture,
            ITestOutputHelper testOutputHelper)
        {
            _assemblyTestLog = AssemblyTestLog.ForAssembly(GetType().Assembly);

            var loggerProvider = _assemblyTestLog.CreateLoggerFactory(testOutputHelper, GetType().Name);

            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(b => b.UseStartup<ErrorPageMiddlewareWebSite.Startup>());
            Client = factory
                .WithWebHostBuilder(builder => builder.ConfigureLogging(l => l.Services.AddSingleton<ILoggerFactory>(loggerProvider)))
                .CreateDefaultClient();
            // These tests want to verify runtime compilation and formatting in the HTML of the error page
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            _fixture = fixture;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CompilationFailuresAreListedByErrorPageMiddleware()
        {
            // Arrange
            var factory = _fixture.Factories.FirstOrDefault() ?? _fixture.WithWebHostBuilder(b => b.UseStartup<ErrorPageMiddlewareWebSite.Startup>());
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
            var expectedCompilationContent = "public class Views_ErrorFromViewImports_Index : "
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

        public void Dispose()
        {
            _assemblyTestLog.Dispose();
        }
    }
}
