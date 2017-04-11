// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestProject
    {
        public static string GetProjectDirectory(Type type)
        {

#if NET452
            var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
#else
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
#endif
            var name = type.GetTypeInfo().Assembly.GetName().Name;

            while (currentDirectory != null &&
                !string.Equals(currentDirectory.Name, name, StringComparison.Ordinal))
            {
                currentDirectory = currentDirectory.Parent;
            }

            return currentDirectory.FullName;
        }
    }
}
