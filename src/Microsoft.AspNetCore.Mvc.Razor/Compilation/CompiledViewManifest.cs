// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public static class CompiledViewManfiest
    {
        public static readonly string PrecompiledViewsAssemblySuffix = ".PrecompiledViews";

        public static Assembly GetFeatureAssembly(AssemblyPart assemblyPart)
        {
            if (assemblyPart.Assembly.IsDynamic || string.IsNullOrEmpty(assemblyPart.Assembly.Location))
            {
                return null;
            }

            var precompiledAssemblyFileName = assemblyPart.Assembly.GetName().Name
                + PrecompiledViewsAssemblySuffix
                + ".dll";
            var precompiledAssemblyFilePath = Path.Combine(
                Path.GetDirectoryName(assemblyPart.Assembly.Location),
                precompiledAssemblyFileName);

            if (File.Exists(precompiledAssemblyFilePath))
            {
                try
                {
                    return Assembly.LoadFile(precompiledAssemblyFilePath);
                }
                catch (FileLoadException)
                {
                    // Don't throw if assembly cannot be loaded. This can happen if the file is not a managed assembly.
                }
            }

            return null;
        }
    }
}
