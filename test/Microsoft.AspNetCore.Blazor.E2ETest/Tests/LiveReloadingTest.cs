// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LiveReloadTestApp;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    // We need an entirely separate test app for the live reloading tests, because
    // otherwise it might break other tests that were running parallel (e.g., if we
    // triggered a reload here while other tests were waiting for something to happen).

    public class LiveReloadingTest
        : ServerTestBase<DevHostServerFixture<LiveReloadTestApp.Program>>
    {
        private const string ServerPathBase = "/live/reloading/subdir";
        private readonly DevHostServerFixture<Program> _serverFixture;

        public LiveReloadingTest(BrowserFixture browserFixture, DevHostServerFixture<Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            _serverFixture = serverFixture;
            serverFixture.Environment = "Development"; // Otherwise the server won't accept live reloading connections
            serverFixture.PathBase = ServerPathBase;
            Navigate(ServerPathBase);
            WaitUntilLoaded();
        }

        [Fact]
        public void ReloadsWhenWebRootFilesAreModified()
        {
            // Verify we have the expected starting point
            var jsFileOutputSelector = By.Id("some-js-file-output");
            Assert.Equal("initial value", Browser.FindElement(jsFileOutputSelector).Text);

            var jsFilePath = Path.Combine(_serverFixture.ContentRoot, "wwwroot", "someJsFile.js");
            var origContents = File.ReadAllText(jsFilePath);
            try
            {
                // Edit the source file on disk
                var newContents = origContents.Replace("'initial value'", "'modified value'");
                File.WriteAllText(jsFilePath, newContents);

                // See that the page reloads and reflects the updated source file
                new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                    driver => driver.FindElement(jsFileOutputSelector).Text == "modified value");
                WaitUntilLoaded();
            }
            finally
            {
                // Restore original state
                File.WriteAllText(jsFilePath, origContents);
            }
        }

        [Fact]
        public void ReloadsWhenBlazorAppRebuilds()
        {
            // Verify we have the expected starting point
            var appElementSelector = By.TagName("app");
            Assert.Equal("Hello, world!", Browser.FindElement(appElementSelector).Text);

            var cshtmlFilePath = Path.Combine(_serverFixture.ContentRoot, "Home.cshtml");
            var origContents = File.ReadAllText(cshtmlFilePath);
            try
            {
                // Edit the source file on disk
                var newContents = origContents.Replace("Hello", "Goodbye");
                File.WriteAllText(cshtmlFilePath, newContents);

                // Trigger build
                var buildConfiguration = DetectBuildConfiguration(_serverFixture.ContentRoot);
                var buildProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build --no-restore --no-dependencies -c {buildConfiguration}",
                    WorkingDirectory = _serverFixture.ContentRoot
                });
                Assert.True(buildProcess.WaitForExit(60 * 1000));
                Assert.Equal(0, buildProcess.ExitCode);

                // See that the page reloads and reflects the updated source file
                new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                    driver => driver.FindElement(appElementSelector).Text == "Goodbye, world!");
            }
            finally
            {
                // Restore original state
                File.WriteAllText(cshtmlFilePath, origContents);
            }
        }

        private object DetectBuildConfiguration(string contentRoot)
        {
            // We want the test to issue the build with the same configuration that
            // the project was already built with (otherwise there will be errors because
            // of having multiple directories under /bin, plus it means we don't need
            // to restore and rebuild all dependencies so it's faster)
            var binDirInfo = new DirectoryInfo(Path.Combine(contentRoot, "bin"));
            var configurationDirs = binDirInfo.GetDirectories();
            Assert.Single(configurationDirs);
            return configurationDirs[0].Name;
        }

        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
