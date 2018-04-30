// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;

namespace Common
{
    public static class Static
    {
        public const string VSTestPrefix = "VSTest: ";
        public const int FlakyProjectColumn = 2260926;

        public const string ProjectKRuntimeEngEmail = "projectk-runtime-eng@microsoft.com";
        public const string BuildBuddyEmail = "rybrande@microsoft.com";

        /// <summary>
        /// This property keeps the various providers from making changes to their data sources when testing things out.
        /// </summary>
        public static bool BeQuite = true;

        public static string FindRepo(string testName)
        {
            if (testName.StartsWith(VSTestPrefix))
            {
                var name = testName.Replace(VSTestPrefix, string.Empty);

                if (name.StartsWith("Microsoft.AspNetCore.Server"))
                {
                    var parts = name.Split('.');
                    switch (parts[3])
                    {
                        case "Kestrel":
                            return "KestrelHttpServer";
                        case "HttpSys":
                            return "HttpSysServer";
                        default:
                            return parts[3];
                    }
                }
                else if (name.StartsWith("Microsoft.AspNetCore."))
                {
                    var parts = name.Split('.');

                    switch (parts[2])
                    {
                        default:
                            return parts[2];
                    }
                }
                else if (name.StartsWith("AuthSamples"))
                {
                    return name.Split('.')[0];
                }
                else if (name.StartsWith("ServerComparison"))
                {
                    return "ServerTests";
                }
                else if (name.StartsWith("E2ETests"))
                {
                    return "MusicStore";
                }
                else if (name.StartsWith("Microsoft.DotNet.Watcher"))
                {
                    return "DotNetTools";
                }
                else if (name.StartsWith("Microsoft.EntityFrameworkCore"))
                {
                    return "EntityFrameworkCore";
                }
                else if (name.StartsWith("FunctionalTests"))
                {
                    return "MvcPrecompilation";
                }
                else if (name.StartsWith("Templates"))
                {
                    return "Templating";
                }
                else if (name.StartsWith("MvcBenchmarks"))
                {
                    return "Performance";
                }
            }

            throw new NotImplementedException($"Don't know how to find the repo of tests like {testName}");
        }

        public static string GetExceptionMessage(string fullErrorMsg)
        {
            // Don't include the stacktrace, it's likely to be different between runs.
            var exceptionMessage = fullErrorMsg.Split(new string[] { "   at " }, StringSplitOptions.RemoveEmptyEntries)[0];
            exceptionMessage = exceptionMessage.Trim();

            // De-uniquify the port
            return Regex.Replace(exceptionMessage, @"127.0.0.1(:\d*)?", "127.0.0.1").Trim();
        }

    }
}
