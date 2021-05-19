// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Testing;
using Moq;
using PlaywrightSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class DownloadAnchorTest
        : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public DownloadAnchorTest(
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(serverFixture, output)
        {            
        }

        protected override Type TestComponent { get; } = typeof(TestRouter);

        [Fact]
        public async Task DownloadFileFromAnchor()
        {
            // Arrange
            var initialUrl = TestPage.Url;
            var downloadTask = TestPage.WaitForEventAsync(PageEvent.Download);

            // Act
            await Task.WhenAll(
                downloadTask,
                TestPage.ClickAsync("a[download]"));

            // Assert URL should still be same as before click
            Assert.Equal(initialUrl, TestPage.Url);

            // Assert that the resource was downloaded            
            var download = downloadTask.Result.Download;
            Assert.Equal($"{_serverFixture.RootUri}subdir/images/blazor_logo_1000x.png", download.Url);
            Assert.Equal("blazor_logo_1000x.png", download.SuggestedFilename);
        }
    }
}
