// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public static class BaselineWriter
{
    private static readonly object baselineLock = new object();

    [Conditional("GENERATE_BASELINES")]
    public static void WriteBaseline(string baselineFile, string output)
    {
        var root = RecursiveFind("Razor.slnf", Path.GetFullPath("."));
        var baselinePath = Path.Combine(root, baselineFile);

        // Serialize writes to minimize contention for file handles and directory access.
        lock (baselineLock)
        {
            // Update baseline
            using (var stream = File.Open(baselinePath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(output);
                }
            }
        }
    }

    private static string RecursiveFind(string path, string start)
    {
        var test = Path.Combine(start, path);
        if (File.Exists(test))
        {
            return start;
        }
        else
        {
            return RecursiveFind(path, new DirectoryInfo(start).Parent.FullName);
        }
    }
}
