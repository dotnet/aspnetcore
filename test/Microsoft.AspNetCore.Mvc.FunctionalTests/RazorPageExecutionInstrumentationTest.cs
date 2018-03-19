// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using RazorPageExecutionInstrumentationWebSite;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RazorPageExecutionInstrumentationTest : IClassFixture<MvcTestFixture<Startup>>
    {
        private static readonly Assembly _resourcesAssembly =
            typeof(RazorPageExecutionInstrumentationTest).GetTypeInfo().Assembly;

        public RazorPageExecutionInstrumentationTest(MvcTestFixture<Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task InstrumentedViews_RenderAsExpected()
        {
            // Arrange
            var outputFile = "compiler/resources/RazorPageExecutionInstrumentationWebSite.Home.ViewWithPartial.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var content = await Client.GetStringAsync("http://localhost/Home/ViewWithPartial");

            // Assert
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, content);
#else
            Assert.Equal(expectedContent, content, ignoreLineEndingDifferences: true);
#endif
        }
    }
}