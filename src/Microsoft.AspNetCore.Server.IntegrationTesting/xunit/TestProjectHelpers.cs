// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.xunit
{
    public class TestProjectHelpers
    {
            public static string GetProjectRoot()
            {
                var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

                var directoryInfo = new DirectoryInfo(applicationBasePath);
                do
                {
                    var projectFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "project.json"));
                    if (projectFileInfo.Exists)
                    {
                        return projectFileInfo.DirectoryName;
                    }

                    directoryInfo = directoryInfo.Parent;
                }
                while (directoryInfo.Parent != null);

                throw new Exception($"Project root could not be found using {applicationBasePath}");
            }
        }
    }
