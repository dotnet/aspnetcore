// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class AssemblyExtension : RazorExtension
    {
        public AssemblyExtension(string extensionName, Assembly assembly)
        {
            if (extensionName == null)
            {
                throw new ArgumentNullException(nameof(extensionName));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            ExtensionName = extensionName;
            Assembly = assembly;
        }

        public override string ExtensionName { get; }

        public Assembly Assembly { get; }
    }
}
