// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.InternalTesting;

[Obsolete("This API is obsolete and the pattern its usage encouraged should not be used anymore. See https://github.com/dotnet/extensions/issues/1697 for details.")]
public class TestPathUtilities
{
    public static string GetSolutionRootDirectory(string solution)
    {
        var applicationBasePath = AppContext.BaseDirectory;
        var directoryInfo = new DirectoryInfo(applicationBasePath);

        do
        {
            var projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{solution}.slnf"));
            if (projectFileInfo.Exists)
            {
                return projectFileInfo.DirectoryName;
            }

            projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "AspNetCore.sln"));
            if (projectFileInfo.Exists)
            {
                // Have reached the solution root. Work down through the src/ folder to find the solution filter.
                directoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "src"));
                foreach (var solutionFileInfo in directoryInfo.EnumerateFiles($"{solution}.slnf", SearchOption.AllDirectories))
                {
                    return solutionFileInfo.DirectoryName;
                }

                // No luck. Exit loop and error out.
                break;
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo.Parent != null);

        throw new Exception($"Solution file {solution}.slnf could not be found in {applicationBasePath} or its parent directories.");
    }
}
