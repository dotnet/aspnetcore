// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Archive
{
    public struct ProgressReport
    {
        public ProgressReport(string phase, long ticks, long total)
        {
            Phase = phase;
            Ticks = ticks;
            Total = total;
        }
        public string Phase { get; }
        public long Ticks { get; }
        public long Total { get; }
    }

    public static class ProgressReportExtensions
    {
        public static void Report(this IProgress<ProgressReport> progress, string phase, long ticks, long total)
        {
            progress.Report(new ProgressReport(phase, ticks, total));
        }
    }

}
