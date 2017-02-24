// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public static class TestProject
    {
        private static readonly string ThisProjectName = typeof(TestProject).GetTypeInfo().Assembly.GetName().Name;

        public static string GetProjectDirectory()
        {

#if NET452
            var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
#else
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
#endif

            while (currentDirectory != null &&
                !string.Equals(currentDirectory.Name, ThisProjectName, StringComparison.Ordinal))
            {
                currentDirectory = currentDirectory.Parent;
            }

            return currentDirectory.FullName;
        }
    }
}
