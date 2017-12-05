// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OpenQA.Selenium;
using System;
using System.IO;
using Xunit;

namespace Blazor.E2ETest.Infrastructure
{
    public class StaticSiteTestBase
        : IClassFixture<BrowserFixture>, IClassFixture<StaticServerFixture>
    {
        public IWebDriver Browser { get; }

        private Uri _serverRootUri;

        public StaticSiteTestBase(
            BrowserFixture browserFixture,
            StaticServerFixture serverFixture,
            string staticSitePath)
        {
            Browser = browserFixture.Browser;

            // Start a static files web server for the specified directory
            var staticSiteFullPath = Path.Combine(FindSolutionDir(), staticSitePath);
            var serverRootUriString = serverFixture.Start(staticSiteFullPath);
            _serverRootUri = new Uri(serverRootUriString);
        }

        public void Navigate(string relativeUrl)
        {
            var absoluteUrl = new Uri(_serverRootUri, relativeUrl);
            Browser.Navigate().GoToUrl(absoluteUrl);
        }

        private string FindSolutionDir()
        {
            return FindClosestDirectoryContaining(
                "Blazor.sln",
                Path.GetDirectoryName(GetType().Assembly.Location));
        }

        private static string FindClosestDirectoryContaining(
            string filename,
            string startDirectory)
        {
            var dir = startDirectory;
            while (true)
            {
                if (File.Exists(Path.Combine(dir, filename)))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
                if (string.IsNullOrEmpty(dir))
                {
                    throw new FileNotFoundException(
                        $"Could not locate a file called '{filename}' in " +
                        $"directory '{startDirectory}' or any parent directory.");
                }
            }
        }
    }
}
