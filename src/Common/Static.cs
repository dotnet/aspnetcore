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

        /// <summary>
        /// Find out what repo the test belongs to based off its namespace.
        /// </summary>
        /// <param name="testName">The full value of the test name as returned by TC.</param>
        /// <returns>The name of the repo the test came from.</returns>
        /// <remarks>We don't have a good way to know what repo a test came out of, so we have this hidious method which attempts to figure it out based on the namespace of the test.</remarks>
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

        /// <summary>
        /// Trim the full value of an error/exception message down to just the message.
        /// </summary>
        /// <param name="fullErrorMsg">The complete error message</param>
        /// <returns>The message of the error.</returns>
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
