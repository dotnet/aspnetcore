// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.xunit
{
    public class TestProjectHelpers
    {
        private const string SolutionName = "Hosting.sln";

        public static string GetSolutionRoot()
        {
            var applicationName = PlatformServices.Default.Application.ApplicationName;
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, SolutionName));
                if (projectFileInfo.Exists)
                {
                    return projectFileInfo.DirectoryName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution file {SolutionName} could not be found using {applicationBasePath}");
        }
    }
}
