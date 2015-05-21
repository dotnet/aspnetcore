// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using PrecompilationWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class PrecompilationTest
    {
        private const string SiteName = nameof(PrecompilationWebSite);
        private static readonly TimeSpan _cacheDelayInterval = TimeSpan.FromSeconds(1);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task PrecompiledView_RendersCorrectly()
        {
            // Arrange
            IServiceCollection serviceCollection = null;
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                _configureServices(services);
                serviceCollection = services;
            });
            var client = server.CreateClient();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();

            var viewsDirectory = Path.Combine(applicationEnvironment.ApplicationBasePath, "Views", "Home");
            var layoutContent = File.ReadAllText(Path.Combine(viewsDirectory, "Layout.cshtml"));
            var indexContent = File.ReadAllText(Path.Combine(viewsDirectory, "Index.cshtml"));
            var viewstartContent = File.ReadAllText(Path.Combine(viewsDirectory, "_ViewStart.cshtml"));
            var globalContent = File.ReadAllText(Path.Combine(viewsDirectory, "_ViewImports.cshtml"));

            // We will render a view that writes the fully qualified name of the Assembly containing the type of
            // the view. If the view is precompiled, this assembly will be PrecompilationWebsite.
            var assemblyNamePrefix = GetAssemblyNamePrefix();

            try
            {
                // Act - 1
                var response = await client.GetAsync("http://localhost/Home/Index");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Assert - 1
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var parsedResponse1 = new ParsedResponse(responseContent);
                Assert.StartsWith(assemblyNamePrefix, parsedResponse1.ViewStart);
                Assert.StartsWith(assemblyNamePrefix, parsedResponse1.Layout);
                Assert.StartsWith(assemblyNamePrefix, parsedResponse1.Index);

                // Act - 2
                // Touch the Layout file and verify it is now dynamically compiled.
                await TouchFile(viewsDirectory, "Layout.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 2
                var response2 = new ParsedResponse(responseContent);
                Assert.StartsWith(assemblyNamePrefix, response2.ViewStart);
                Assert.StartsWith(assemblyNamePrefix, response2.Index);
                Assert.DoesNotContain(assemblyNamePrefix, response2.Layout);

                // Act - 3
                // Touch the _ViewStart file and verify it is is dynamically compiled.
                await TouchFile(viewsDirectory, "_ViewStart.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 3
                var response3 = new ParsedResponse(responseContent);
                Assert.NotEqual(assemblyNamePrefix, response3.ViewStart);
                Assert.Equal(response2.Index, response3.Index);
                Assert.Equal(response2.Layout, response3.Layout);

                // Act - 4
                // Touch the _ViewImports file and verify it causes all files to recompile.
                await TouchFile(viewsDirectory, "_ViewImports.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 4
                var response4 = new ParsedResponse(responseContent);
                Assert.NotEqual(response3.ViewStart, response4.ViewStart);
                Assert.NotEqual(response3.Index, response4.Index);
                Assert.NotEqual(response3.Layout, response4.Layout);

                // Act - 5
                // Touch Index file and verify it is the only page that recompiles.
                await TouchFile(viewsDirectory, "Index.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 5
                var response5 = new ParsedResponse(responseContent);
                // Layout and _ViewStart should not have changed.
                Assert.Equal(response4.Layout, response5.Layout);
                Assert.Equal(response4.ViewStart, response5.ViewStart);
                Assert.NotEqual(response4.Index, response5.Index);

                // Act - 6
                // Touch the _ViewImports file. This time, we'll verify the Non-precompiled -> Non-precompiled workflow.
                await TouchFile(viewsDirectory, "_ViewImports.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 6
                var response6 = new ParsedResponse(responseContent);
                // Everything should've recompiled.
                Assert.NotEqual(response5.ViewStart, response6.ViewStart);
                Assert.NotEqual(response5.Index, response6.Index);
                Assert.NotEqual(response5.Layout, response6.Layout);

                // Act - 7
                // Add a new _ViewImports file
                var newViewImports = await TouchFile(Path.GetDirectoryName(viewsDirectory), "_ViewImports.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 7
                // Everything should've recompiled.
                var response7 = new ParsedResponse(responseContent);
                Assert.NotEqual(response6.ViewStart, response7.ViewStart);
                Assert.NotEqual(response6.Index, response7.Index);
                Assert.NotEqual(response6.Layout, response7.Layout);

                // Act - 8
                // Remove new _ViewImports file
                File.Delete(newViewImports);
                await Task.Delay(_cacheDelayInterval);
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 8
                // Everything should've recompiled.
                var response8 = new ParsedResponse(responseContent);
                Assert.NotEqual(response7.ViewStart, response8.ViewStart);
                Assert.NotEqual(response7.Index, response8.Index);
                Assert.NotEqual(response7.Layout, response8.Layout);

                // Act - 9
                // Refetch and verify we get cached types
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 9
                var response9 = new ParsedResponse(responseContent);
                Assert.Equal(response8.ViewStart, response9.ViewStart);
                Assert.Equal(response8.Index, response9.Index);
                Assert.Equal(response8.Layout, response9.Layout);
            }
            finally
            {
                File.WriteAllText(Path.Combine(viewsDirectory, "Layout.cshtml"), layoutContent.TrimEnd(' '));
                File.WriteAllText(Path.Combine(viewsDirectory, "Index.cshtml"), indexContent.TrimEnd(' '));
                File.WriteAllText(Path.Combine(viewsDirectory, "_ViewStart.cshtml"), viewstartContent.TrimEnd(' '));
                File.WriteAllText(Path.Combine(viewsDirectory, "_ViewImports.cshtml"), globalContent.TrimEnd(' '));
            }
        }

        [Fact]
        public async Task PrecompiledView_UsesCompilationOptionsFromApplication()
        {
            // Arrange
            var assemblyNamePrefix = GetAssemblyNamePrefix();
#if DNX451
            var expected =
@"Value set inside DNX451 " + assemblyNamePrefix;
#elif DNXCORE50
            var expected =
@"Value set inside DNXCORE50 " + assemblyNamePrefix;
#endif

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/PrecompiledViewsCanConsumeCompilationOptions");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.StartsWith(expected, responseContent.Trim());
        }

        [Fact]
        public async Task DeletingPrecompiledGlobalFile_PriorToFirstRequestToAView_CausesViewToBeRecompiled()
        {
            // Arrange
            var expected = GetAssemblyNamePrefix();
            IServiceCollection serviceCollection = null;
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                _configureServices(services);
                serviceCollection = services;
            });
            var client = server.CreateClient();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();

            var viewsDirectory = Path.Combine(applicationEnvironment.ApplicationBasePath,
                                              "Views",
                                              "ViewImportsDelete");
            var globalPath = Path.Combine(viewsDirectory, "_ViewImports.cshtml");
            var globalContent = File.ReadAllText(globalPath);

            // Act - 1
            // Query the Test view so we know the compiler cache gets populated.
            var response = await client.GetStringAsync("/Test");

            // Assert - 1
            Assert.Equal("Test", response.Trim());

            try
            {
                // Act - 2
                File.Delete(globalPath);
                var response2 = await client.GetStringAsync("http://localhost/Home/GlobalDeletedPriorToFirstRequest");

                // Assert - 2
                Assert.DoesNotContain(expected, response2.Trim());
            }
            finally
            {
                File.WriteAllText(globalPath, globalContent);
            }
        }

        [Fact]
        public async Task TagHelpersFromTheApplication_CanBeAdded()
        {
            // Arrange
            var assemblyNamePrefix = GetAssemblyNamePrefix();
            var expected = 
                @"<root data-root=""true""><input class=""form-control"" type=""number"" data-val=""true""" +
                @" data-val-range=""The field Age must be between 10 and 100."" data-val-range-max=""100"" "+
                @"data-val-range-min=""10"" data-val-required=""The Age field is required."" " +
                @"id=""Age"" name=""Age"" value="""" /><a href="""">Back to List</a></root>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TagHelpers/Add");

            // Assert
            var responseLines = response.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith(assemblyNamePrefix, responseLines[0]);
            Assert.Equal(expected, responseLines[1]);
        }

        [Fact]
        public async Task TagHelpersFromTheApplication_CanBeRemoved()
        {
            // Arrange
            var assemblyNamePrefix = GetAssemblyNamePrefix();
            var expected = @"<root>root-content</root>";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TagHelpers/Remove");

            // Assert
            var responseLines = response.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith(assemblyNamePrefix, responseLines[0]);
            Assert.Equal(expected, responseLines[1]);
        }

        private static string GetAssemblyNamePrefix()
        {
            return typeof(Startup).GetTypeInfo().Assembly.GetName().Name + "." + nameof(RazorPreCompiler) + ".";
        }

        private static async Task<string> TouchFile(string viewsDir, string file)
        {
            var path = Path.Combine(viewsDir, file);
            File.AppendAllText(path, " ");

            // Delay to allow the file system watcher to catch up.
            await Task.Delay(_cacheDelayInterval);

            return path;
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
