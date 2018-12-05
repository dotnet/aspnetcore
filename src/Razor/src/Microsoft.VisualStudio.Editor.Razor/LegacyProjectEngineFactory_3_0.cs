// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [ExportCustomProjectEngineFactory("MVC-3.0", SupportsSerialization = true)]
    internal class LegacyProjectEngineFactory_3_0 : IProjectEngineFactory
    {
        private const string AssemblyName = "Microsoft.AspNetCore.Mvc.Razor.Extensions";
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            // Rewrite the assembly name into a full name just like this one, but with the name of the MVC design time assembly.
            var assemblyName = new AssemblyName(typeof(LegacyProjectEngineFactory_3_0).Assembly.FullName);
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
