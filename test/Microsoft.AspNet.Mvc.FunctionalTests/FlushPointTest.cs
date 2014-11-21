// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FlushPointTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("RazorWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task FlushPointsAreExecutedForPagesWithLayouts()
        {
            var waitService = new WaitService();
            var serviceProvider = GetServiceProvider(waitService);
            var server = TestServer.Create(serviceProvider, _app);
            var client = server.CreateClient();

            // Act
            var stream = await client.GetStreamAsync("http://localhost/FlushPoint/PageWithLayout");

            // Assert - 1
            Assert.Equal(@"<title>Page With Layout</title>", GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 2
            Assert.Equal(@"RenderBody content", GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 3
            Assert.Equal(@"<span>Content that takes time to produce</span>",
                        GetTrimmedString(stream));
        }

        [Fact]
        public async Task FlushPointsAreExecutedForPagesWithoutLayouts()
        {
            var waitService = new WaitService();
            var serviceProvider = GetServiceProvider(waitService);

            var server = TestServer.Create(serviceProvider, _app);
            var client = server.CreateClient();

            // Act
            var stream = await client.GetStreamAsync("http://localhost/FlushPoint/PageWithoutLayout");

            // Assert - 1
            Assert.Equal("Initial content", GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 2
            Assert.Equal("Secondary content", GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 3
            Assert.Equal("Final content", GetTrimmedString(stream));
        }

        [Theory]
        [InlineData("PageWithPartialsAndViewComponents", "FlushAsync invoked inside RenderSection")]
        [InlineData("PageWithRenderSectionAsync", "FlushAsync invoked inside RenderSectionAsync")]
        public async Task FlushPointsAreExecutedForPagesWithComponentsPartialsAndSections(string action, string title)
        {
            var waitService = new WaitService();
            var serviceProvider = GetServiceProvider(waitService);

            var server = TestServer.Create(serviceProvider, _app);
            var client = server.CreateClient();

            // Act
            var stream = await client.GetStreamAsync("http://localhost/FlushPoint/" + action);

            // Assert - 1
            Assert.Equal(string.Join(Environment.NewLine,
                                     "<title>" + title + "</title>",
                                     "",
                                     "RenderBody content"), GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 2
            Assert.Equal(string.Join(
                Environment.NewLine,
                "partial-content",
                "",
                "Value from TaskReturningString",
                "<p>section-content</p>"), GetTrimmedString(stream));
            waitService.WaitForServer();

            // Assert - 3
            Assert.Equal(string.Join(
                Environment.NewLine,
                "component-content",
                "    <span>Content that takes time to produce</span>",
                "",
                "More content from layout"), GetTrimmedString(stream));
        }

        private IServiceProvider GetServiceProvider(WaitService waitService)
        {
            var services = new ServiceCollection();
            services.AddInstance(waitService);
            return TestHelper.CreateServices("RazorWebSite", services);
        }

        private string GetTrimmedString(Stream stream)
        {
            var buffer = new byte[1024];
            var count = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, count).Trim();
        }
    }
}
