// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class ExtensionAssemblyLoader
    {
        public abstract void AddAssemblyLocation(string filePath);

        public abstract Assembly Load(string assemblyName);

        public abstract Assembly LoadFromPath(string filePath);
    }
}