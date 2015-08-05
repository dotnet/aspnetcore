// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Dnx.Runtime;
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

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
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
            var indexContent = File.ReadAllText(Path.Combine(viewsDirectory, "Index.cshtml"));

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
                // Touch the Index file and verify it remains unaffected.
                await TouchFile(viewsDirectory, "Index.cshtml");
                responseContent = await client.GetStringAsync("http://localhost/Home/Index");

                // Assert - 2
                var response2 = new ParsedResponse(responseContent);
                Assert.StartsWith(assemblyNamePrefix, response2.ViewStart);
                Assert.StartsWith(assemblyNamePrefix, response2.Index);
                Assert.StartsWith(assemblyNamePrefix, response2.Layout);
            }
            finally
            {
                File.WriteAllText(Path.Combine(viewsDirectory, "Index.cshtml"), indexContent.TrimEnd(' '));
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

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
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
            var responseLines = response.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith(assemblyNamePrefix, responseLines[0]);
            Assert.Equal(expected, responseLines[1]);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
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
            var responseLines = response.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
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
                var results = responseContent
                    .Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
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
