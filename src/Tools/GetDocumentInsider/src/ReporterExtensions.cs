// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal static class ReporterExtensions
    {
        public static bool PrefixOutput { get; set; }

        public static void WriteError(this IReporter reporter, string message)
            => reporter.Error(Prefix("error:   ", message));

        public static void WriteWarning(this IReporter reporter, string message)
            => reporter.Warn(Prefix("warn:    ", message));

        public static void WriteInformation(this IReporter reporter, string message)
            => reporter.Output(Prefix("info:    ", message));

        public static void WriteVerbose(this IReporter reporter, string message)
            => reporter.Verbose(Prefix("verbose: ", message));

        private static string Prefix(string prefix, string value)
        {
            if (PrefixOutput)
            {
                return string.Join(
                    Environment.NewLine,
                    value
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                        .Select(l => prefix + l));
            }

            return value;
        }
    }
}
