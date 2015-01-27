// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using PrecompilationWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class PrecompilationTest
    {
        private static readonly TimeSpan _cacheDelayInterval = TimeSpan.FromSeconds(2);
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(PrecompilationWebSite));
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task PrecompiledView_RendersCorrectly()
        {
            // Arrange
            var applicationEnvironment = _services.GetRequiredService<IApplicationEnvironment>();

            var viewsDirectory = Path.Combine(applicationEnvironment.ApplicationBasePath, "Views", "Home");
            var layoutContent = File.ReadAllText(Path.Combine(viewsDirectory, "Layout.cshtml"));
            var indexContent = File.ReadAllText(Path.Combine(viewsDirectory, "Index.cshtml"));
            var viewstartContent = File.ReadAllText(Path.Combine(viewsDirectory, "_ViewStart.cshtml"));

            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // We will render a view that writes the fully qualified name of the Assembly containing the type of
            // the view. If the view is precompiled, this assembly will be PrecompilationWebsite.
            var assemblyName = typeof(Startup).GetTypeInfo().Assembly.GetName().ToString();

            try
            {
                // Act - 1
                var response = await client.GetAsync("http://localhost/Home/Index");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Assert - 1
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var parsedResponse1 = new ParsedResponse(responseContent);
                Assert.Equal(assemblyName, parsedResponse1.ViewStart);
                Assert.Equal(assemblyName, parsedResponse1.Layout);
                Assert.Equal(assemblyName, parsedResponse1.Index);

                // Act - 2
                // Touch the Layout file and verify it is now dynamically compiled.
                await TouchFile(viewsDirectory, "Layout.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 2
                var response2 = new ParsedResponse(responseContent);
                Assert.NotEqual(assemblyName, response2.Layout);
                Assert.Equal(assemblyName, response2.ViewStart);
                Assert.Equal(assemblyName, response2.Index);

                // Act - 3
                // Touch the _ViewStart file and verify it causes all files to recompile.
                await TouchFile(viewsDirectory, "_ViewStart.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 3
                var response3 = new ParsedResponse(responseContent);
                Assert.NotEqual(assemblyName, response3.ViewStart);
                Assert.NotEqual(assemblyName, response3.Index);
                Assert.NotEqual(response2.Layout, response3.Layout);

                // Act - 4
                // Touch Index file and verify it is the only page that recompiles.
                await TouchFile(viewsDirectory, "Index.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 4
                var response4 = new ParsedResponse(responseContent);
                // Layout and _ViewStart should not have changed.
                Assert.Equal(response3.Layout, response4.Layout);
                Assert.Equal(response3.ViewStart, response4.ViewStart);
                Assert.NotEqual(response3.Index, response4.Index);

                // Act - 5
                // Touch the _ViewStart file. This time, we'll verify the Non-precompiled -> Non-precompiled workflow.
                await TouchFile(viewsDirectory, "_ViewStart.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 5
                var response5 = new ParsedResponse(responseContent);
                // Everything should've recompiled.
                Assert.NotEqual(response4.ViewStart, response5.ViewStart);
                Assert.NotEqual(response4.Index, response5.Index);
                Assert.NotEqual(response4.Layout, response5.Layout);

                // Act - 6
                // Add a new _ViewStart file
                File.WriteAllText(Path.Combine(viewsDirectory, "..", "_ViewStart.cshtml"), string.Empty);
                await Task.Delay(_cacheDelayInterval);
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 6
                // Everything should've recompiled.
                var response6 = new ParsedResponse(responseContent);
                Assert.NotEqual(response5.ViewStart, response6.ViewStart);
                Assert.NotEqual(response5.Index, response6.Index);
                Assert.NotEqual(response5.Layout, response6.Layout);

                // Act - 7
                // Remove new _ViewStart file
                File.Delete(Path.Combine(viewsDirectory, "..", "_ViewStart.cshtml"));
                await Task.Delay(_cacheDelayInterval);
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 7
                // Everything should've recompiled.
                var response7 = new ParsedResponse(responseContent);
                Assert.NotEqual(response6.ViewStart, response7.ViewStart);
                Assert.NotEqual(response6.Index, response7.Index);
                Assert.NotEqual(response6.Layout, response7.Layout);

                // Act - 8
                // Refetch and verify we get cached types
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 7
                var response8 = new ParsedResponse(responseContent);
                Assert.Equal(response7.ViewStart, response8.ViewStart);
                Assert.Equal(response7.Index, response8.Index);
                Assert.Equal(response7.Layout, response8.Layout);
            }
            finally
            {
                File.WriteAllText(Path.Combine(viewsDirectory, "Layout.cshtml"), layoutContent);
                File.WriteAllText(Path.Combine(viewsDirectory, "Index.cshtml"), indexContent);
                File.WriteAllText(Path.Combine(viewsDirectory, "_ViewStart.cshtml"), viewstartContent);
            }
        }

        [Fact]
        public async Task PrecompiledView_UsesCompilationOptionsFromApplication()
        {
            // Arrange
            var assemblyName = typeof(Startup).GetTypeInfo().Assembly.GetName().ToString();
#if ASPNET50
            var expected =
@"Value set inside ASPNET50 " + assemblyName;
#elif ASPNETCORE50
            var expected =
@"Value set inside ASPNETCORE50 " + assemblyName;
#endif

            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/PrecompiledViewsCanConsumeCompilationOptions");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, responseContent.Trim());
        }

        [Fact]
        public async Task DeletingPrecompiledViewStart_PriorToFirstRequestToAView_CausesViewToBeRecompiled()
        {
            // Arrange
            var expected = typeof(Startup).GetTypeInfo().Assembly.GetName().ToString();
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var applicationEnvironment = _services.GetRequiredService<IApplicationEnvironment>();

            var viewsDirectory = Path.Combine(applicationEnvironment.ApplicationBasePath, "Views", "ViewStartDelete");
            var viewStartPath = Path.Combine(viewsDirectory, "_ViewStart.cshtml");
            var viewStartContent = File.ReadAllText(viewStartPath);

            // Act - 1
            // Query the Test view so we know the compiler cache gets populated.
            var response = await client.GetStringAsync("/Test");

            // Assert - 1
            Assert.Equal("Test", response.Trim());

            try
            {
                // Act - 2
                var response2 = await client.GetStringAsync("http://localhost/Home/ViewStartDeletedPriorToFirstRequest");

                // Assert - 2
                Assert.NotEqual(expected, response2.Trim());
            }
            finally
            {
                File.WriteAllText(viewStartPath, viewStartContent);
            }
        }

        private static Task TouchFile(string viewsDir, string file)
        {
            File.AppendAllText(Path.Combine(viewsDir, file), " ");
            // Delay to ensure we don't hit the cached file system.
            return Task.Delay(_cacheDelayInterval);
        }

        private sealed class ParsedResponse
        {
            public ParsedResponse(string responseContent)
            {
                var results = responseContent.Split(new[] { Environment.NewLine },
                                                    StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim())
                                             .ToArray();

                Assert.True(results[0].StartsWith("Layout:"));
                Layout = results[0].Substring("Layout:".Length);

                Assert.True(results[1].StartsWith("_viewstart:"));
                ViewStart = results[1].Substring("_viewstart:".Length);

                Assert.True(results[2].StartsWith("index:"));
                Index = results[2].Substring("index:".Length);
            }

            public string Layout { get; }

            public string ViewStart { get; }

            public string Index { get; }
        }

    }
}