// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    // Currently we provide a fixed configuration for 2.1, but this is a point-in-time issue. We plan
    // to make the 2.1 configuration more flexible and less hardcoded.
    [ExportCustomProjectEngineFactory("MVC-2.1", SupportsSerialization = true)]
    internal class LegacyProjectEngineFactory_2_1 : IProjectEngineFactory
    {
        private const string AssemblyName = "Microsoft.AspNetCore.Mvc.Razor.Extensions";
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            // Rewrite the assembly name into a full name just like this one, but with the name of the MVC design time assembly.
            var assemblyName = new AssemblyName(typeof(LegacyProjectEngineFactory_2_1).Assembly.FullName);
            assemblyName.Name = AssemblyName;

            var extension = new AssemblyExtension(configuration.ConfigurationName, Assembly.Load(assemblyName));
            var initializer = extension.CreateInitializer();

            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                initializer.Initialize(b);
                configure?.Invoke(b);
            });
        }
    }
}
