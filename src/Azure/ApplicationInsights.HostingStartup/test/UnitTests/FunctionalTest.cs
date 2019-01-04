// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace ApplicationInsightsJavaScriptSnippetTest
{
    public class ApplicationInsightsFunctionalTest : LoggedTest
    {
        public ApplicationInsightsFunctionalTest(ITestOutputHelper output) : base(output)
        {
        }

        protected static string GetApplicationPath()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Azure.sln")))
                {
                    break;
                }
                current = current.Parent;
            }

            if (current == null)
            {
                throw new InvalidOperationException("Could not find the solution directory");
            }

            return Path.GetFullPath(Path.Combine(current.FullName, "ApplicationInsights.HostingStartup", "test", "testassets", "ApplicationInsightsHostingStartupSample"));
        }

        protected static bool PreservePublishedApplicationForDebugging
        {
            get
            {
                var deletePublishedFolder = Environment.GetEnvironmentVariable("ASPNETCORE_DELETEPUBLISHEDFOLDER");

                if (string.Equals("false", deletePublishedFolder, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("0", deletePublishedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // preserve the published folder and do not delete it
                    return true;
                }

                // do not preserve the published folder and delete it
                return false;
            }
        }

        protected static string GetCurrentBuildConfiguration()
        {
            var configuration = "Debug";
            if (string.Equals(Environment.GetEnvironmentVariable("Configuration"), "Release", StringComparison.OrdinalIgnoreCase))
            {
                configuration = "Release";
            }

            return configuration;
        }
    }
}
