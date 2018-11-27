// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Blazor
{
    // This factory is wired up based on a match of the $(DefaultRazorConfiguration) property is MSBuild
    // keep this in sync with the supported runtime/build version.
    [ExportCustomProjectEngineFactory("Blazor-0.1", SupportsSerialization = false)]
    internal class BlazorProjectEngineFactory : IProjectEngineFactory
    {
        public RazorProjectEngine Create(RazorConfiguration configuration, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            return RazorProjectEngine.Create(configuration, fileSystem, b =>
            {
                configure?.Invoke(b);
                new BlazorExtensionInitializer().Initialize(b);

                var classifier = b.Features.OfType<ComponentDocumentClassifierPass>().Single();
                classifier.MangleClassNames = true;
            });
        }
    }
}
