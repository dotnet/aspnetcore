// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Common
{
    public static class TestToAreaMapper
    {
        private static readonly List<(string testPrefix, string area)?> TestPrefixToAreaMapping =
            new List<(string testPrefix, string area)?>
            {
                // Extensions
                ("Microsoft.Extensions.Configuration", "area-configuration"),
                ("Microsoft.Extensions.Primitives", "area-common"),
                ("Microsoft.Extensions.Options", "area-options"),
                ("Microsoft.Extensions.Caching", "area-caching"),

                // EF
                ("Microsoft.EntityFrameworkCore", "area-ef"),
                ("Microsoft.Data.Sqlite", "area-ef"),

                // ASP.NET
                ("Microsoft.AspNetCore.Authentication", "area-security"),
                ("Microsoft.AspNetCore.Http", "area-servers"),
                ("Microsoft.AspNetCore.Mvc", "area-mvc"),
                ("Microsoft.AspNetCore.Server", "area-servers"),
                ("Microsoft.AspNetCore.SignalR", "area-signalr"),
                ("Microsoft.AspNetCore.FunctionalTests.Microsoft.AspNetCore.Tests.WebHostFunctionalTests", "area-platform"),
                ("Microsoft.Extensions.Http", "area-mvc"),
                ("Microsoft.Extensions.Internal.Test", "area-servers"),
                ("System.Buffers.Tests", "area-servers"),
                ("System.IO.Pipelines.Tests", "area-servers"),
                ("AuthSamples", "area-security"),
                ("E2ETests", "area-mvc"),
                ("FunctionalTests", "area-mvc"),
                ("IIS.FunctionalTests", "area-servers"),
                ("MvcBenchmarks", "area-mvc"),
                ("ServerComparison", "area-servers"),
                ("Templates", "area-tools"),
                ("Microsoft.DotNet.Watcher", "area-tools"),
                ("Microsoft.AspNetCore.Identity", "area-identity"),
                ("Microsoft.VisualStudio.Editor.Razor", "area-mvc"),
                ("Microsoft.AspNetCore.Razor", "area-mvc"),

                // Tools
                ("Microsoft.VisualStudio.Web.CodeGeneration", "area-webtools"),
            };


        /// <summary>
        /// Find out what product area the test belongs to based off its namespace.
        /// </summary>
        /// <param name="testName">The full value of the test name as returned by TC.</param>
        /// <param name="reporter">The reporter to use.</param>
        /// <returns>The name of the area the test came from, for example, 'area-mvc'.</returns>
        /// <remarks>We don't have a good way to know what repo a test came out of, so we have this hidious method which attempts to figure it out based on the namespace of the test.</remarks>
        public static string FindTestProductArea(string testName, IReporter reporter)
        {
            var name = testName.Replace(Constants.VSTestPrefix, string.Empty);

            var area = TestPrefixToAreaMapping
                .FirstOrDefault(map => name.StartsWith(map?.testPrefix, StringComparison.OrdinalIgnoreCase))?.area;

            if (area == null)
            {
                reporter.Warn($"Don't know how to find the area of tests like '{testName}', so an area label will not be applied");
            }

            return area;
        }
    }
}
