// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Testing
{
    [Obsolete("This API is obsolete and the pattern its usage encouraged should not be used anymore. See https://github.com/dotnet/extensions/issues/1697 for details.")]
    public class TestPathUtilities
    {
        public static string GetRepoRootDirectory()
        {
            return GetSolutionRootDirectory("Extensions");
        }

        public static string GetSolutionRootDirectory(string solution)
        {
            var applicationBasePath = AppContext.BaseDirectory;
            var directoryInfo = new DirectoryInfo(applicationBasePath);

            do
            {
                var projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{solution}.sln"));
                if (projectFileInfo.Exists)
                {
                    return projectFileInfo.DirectoryName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution file {solution}.sln could not be found in {applicationBasePath} or its parent directories.");
        }
    }
}
