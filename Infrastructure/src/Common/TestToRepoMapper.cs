// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using McMaster.Extensions.CommandLineUtils;

namespace Common
{
    public static class TestToRepoMapper
    {
        /// <summary>
        /// Find out what repo the test belongs to based off its namespace.
        /// </summary>
        /// <param name="testName">The full value of the test name as returned by TC.</param>
        /// <param name="reporter">The reporter to use.</param>
        /// <returns>The name of the repo the test came from.</returns>
        /// <remarks>We don't have a good way to know what repo a test came out of, so we have this hidious method which attempts to figure it out based on the namespace of the test.</remarks>
        public static string FindRepo(string testName, IReporter reporter)
        {
            if (Constants.BeQuiet)
            {
                return "TriageTest";
            }
            else
            {
                var name = testName.Replace(Constants.VSTestPrefix, string.Empty);

                if (name.StartsWith("Microsoft.AspNetCore.Server", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = name.Split('.');
                    switch (parts[3])
                    {
                        case "Kestrel":
                            return "KestrelHttpServer";
                        case "HttpSys":
                            return "HttpSysServer";
                        case "IIS":
                            return "IISIntegration";
                        default:
                            return parts[3];
                    }
                }
                else if (name.StartsWith("Microsoft.AspNetCore.Http"))
                {
                    return "HttpAbstractions";
                }
                else if (name.StartsWith("Microsoft.AspNetCore.Authentication"))
                {
                    return "Security";
                }
                else if (name.StartsWith("Microsoft.AspNetCore.FunctionalTests.Microsoft.AspNetCore.Tests.WebHostFunctionalTests"))
                {
                    return "MetaPackages";
                }
                else if (name.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = name.Split('.');

                    switch (parts[2])
                    {
                        default:
                            return parts[2];
                    }
                }
                else if (name.StartsWith("AuthSamples", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Split('.')[0];
                }
                else if (name.StartsWith("Microsoft.Extensions.Configuration", StringComparison.OrdinalIgnoreCase))
                {
                    return "Configuration";
                }
                else if (name.StartsWith("ServerComparison", StringComparison.OrdinalIgnoreCase))
                {
                    return "ServerTests";
                }
                else if (name.StartsWith("E2ETests", StringComparison.OrdinalIgnoreCase))
                {
                    return "MusicStore";
                }
                else if (name.StartsWith("Microsoft.DotNet.Watcher", StringComparison.OrdinalIgnoreCase))
                {
                    return "DotNetTools";
                }
                else if (name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                {
                    return "EntityFrameworkCore";
                }
                else if (name.StartsWith("FunctionalTests", StringComparison.OrdinalIgnoreCase))
                {
                    return "MvcPrecompilation";
                }
                else if (name.StartsWith("Templates", StringComparison.OrdinalIgnoreCase))
                {
                    return "Templating";
                }
                else if (name.StartsWith("MvcBenchmarks", StringComparison.OrdinalIgnoreCase))
                {
                    return "Performance";
                }
                else if (name.StartsWith("Microsoft.VisualStudio.Web.CodeGeneration", StringComparison.OrdinalIgnoreCase))
                {
                    return "Scaffolding";
                }
                else if (name.StartsWith("Microsoft.Data.Sqlite"))
                {
                    return "Microsoft.Data.Sqlite";
                }
                else if (name.StartsWith("IIS.FunctionalTests"))
                {
                    return "IISIntegration";
                }
                else if (name.StartsWith("Microsoft.Extensions.Primitives"))
                {
                    return "Common";
                }
                else if (name.StartsWith("Microsoft.Extensions.Options"))
                {
                    return "Options";
                }
                else if (name.StartsWith("Microsoft.Extensions.Configuration", StringComparison.OrdinalIgnoreCase))
                {
                    return "Configuration";
                }
                else if (name.StartsWith("ServerComparison", StringComparison.OrdinalIgnoreCase))
                {
                    return "ServerTests";
                }
                else if (name.StartsWith("E2ETests", StringComparison.OrdinalIgnoreCase))
                {
                    return "MusicStore";
                }
                else if (name.StartsWith("Microsoft.DotNet.Watcher", StringComparison.OrdinalIgnoreCase))
                {
                    return "DotNetTools";
                }
                else if (name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                {
                    return "EntityFrameworkCore";
                }
                else if (name.StartsWith("FunctionalTests", StringComparison.OrdinalIgnoreCase))
                {
                    return "MvcPrecompilation";
                }
                else if (name.StartsWith("Templates", StringComparison.OrdinalIgnoreCase))
                {
                    return "Templating";
                }
                else if (name.StartsWith("MvcBenchmarks", StringComparison.OrdinalIgnoreCase))
                {
                    return "Performance";
                }
                else if (name.StartsWith("Microsoft.VisualStudio.Web.CodeGeneration", StringComparison.OrdinalIgnoreCase))
                {
                    return "Scaffolding";
                }
                else if (name.StartsWith("Microsoft.Data.Sqlite"))
                {
                    return "Microsoft.Data.Sqlite";
                }
                else if (name.StartsWith("IIS.FunctionalTests"))
                {
                    return "IISIntegration";
                }
                else if (name.StartsWith("Microsoft.Extensions.Options"))
                {
                    return "Options";
                }
                else if (name.StartsWith("Microsoft.Extensions.Caching"))
                {
                    return "Caching";
                }
                else if (name.StartsWith("Microsoft.Extensions.Http"))
                {
                    return "HttpClientFactory";
                }
                else if (name.StartsWith("System.Buffers.Tests", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("System.IO.Pipelines.Tests", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Microsoft.Extensions.Internal.Test", StringComparison.OrdinalIgnoreCase))
                {
                    return "AspNetCore";
                }

                reporter.Warn($"Don't know how to find the repo of tests like {testName}, defaulting to aspnet/AspNetCore");

                return "AspNetCore";
            }
        }

        public static string FindOwner(string name)
        {
            if (Constants.BeQuiet)
            {
                return "ryanbrandenburg";
            }
            else
            {
                return "aspnet";
            }
        }
    }
}
