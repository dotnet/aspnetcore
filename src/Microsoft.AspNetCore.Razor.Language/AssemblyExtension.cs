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

        public RazorExtensionInitializer CreateInitializer()
        {
            // It's not an error to have an assembly with no initializers. This is useful to specify a dependency
            // that doesn't really provide any Razor configuration.
            var attributes = Assembly.GetCustomAttributes<ProvideRazorExtensionInitializerAttribute>();
            foreach (var attribute in attributes)
            {
                // Using extension names and requiring them to line up allows a single assembly to ship multiple
                // extensions/initializers for different configurations.
                if (!string.Equals(attribute.ExtensionName, ExtensionName, StringComparison.Ordinal))
                {
                    continue;
                }

                // There's no real protection/exception handling here because this set isn't really user-extensible
                // right now. This would be a great place to add some additional diagnostics and hardening in the
                // future.
                var initializer = (RazorExtensionInitializer)Activator.CreateInstance(attribute.InitializerType);
                return initializer;
            }

            return null;
        }
    }
}
