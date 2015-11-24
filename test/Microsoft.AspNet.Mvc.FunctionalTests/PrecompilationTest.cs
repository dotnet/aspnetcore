// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.AspNet.Testing.xunit;
using PrecompilationWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class PrecompilationTest : IClassFixture<MvcTestFixture<PrecompilationWebSite.Startup>>
    {
        public PrecompilationTest(MvcTestFixture<PrecompilationWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task PrecompiledView_RendersCorrectly()
        {
            // Arrange
            // We will render a view that writes the fully qualified name of the Assembly containing the type of
            // the view. If the view is precompiled, this assembly will be PrecompilationWebsite.
            var assemblyNamePrefix = GetAssemblyNamePrefix();

            // Act
            var response = await Client.GetAsync("http://localhost/Home/Index");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var parsedResponse1 = new ParsedResponse(responseContent);
            Assert.StartsWith(assemblyNamePrefix, parsedResponse1.ViewStart);
            Assert.StartsWith(assemblyNamePrefix, parsedResponse1.Layout);
            Assert.StartsWith(assemblyNamePrefix, parsedResponse1.Index);
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

            // Act
            var response = await Client.GetAsync("http://localhost/Home/PrecompiledViewsCanConsumeCompilationOptions");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.StartsWith(expected, responseContent.Trim());
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task TagHelpersFromTheApplication_CanBeAdded()
        {
            // Arrange
            var assemblyNamePrefix = GetAssemblyNamePrefix();
            var expected =
                @"<root data-root=""true""><input class=""form-control"" type=""number"" data-val=""true""" +
                @" data-val-range=""The field Age must be between 10 and 100."" data-val-range-max=""100"" "+
                @"data-val-range-min=""10"" data-val-required=""The Age field is required."" " +
                @"id=""Age"" name=""Age"" value="""" /><a href=""/TagHelpers"">Back to List</a></root>";

            // Act
            var response = await Client.GetStringAsync("http://localhost/TagHelpers/Add");

            // Assert
            var responseLines = response.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith(assemblyNamePrefix, responseLines[0]);
            Assert.Equal(expected, responseLines[1]);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task TagHelpersFromTheApplication_CanBeRemoved()
        {
            // Arrange
            var assemblyNamePrefix = GetAssemblyNamePrefix();
            var expected = @"<root>root-content</root>";

            // Act
            var response = await Client.GetStringAsync("http://localhost/TagHelpers/Remove");

            // Assert
            var responseLines = response.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith(assemblyNamePrefix, responseLines[0]);
            Assert.Equal(expected, responseLines[1]);
        }

        private static string GetAssemblyNamePrefix()
        {
            return typeof(Startup).GetTypeInfo().Assembly.GetName().Name + "." + nameof(RazorPreCompiler) + ".";
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
