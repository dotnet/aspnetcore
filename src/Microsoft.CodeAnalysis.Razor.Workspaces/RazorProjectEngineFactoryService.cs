// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class RazorProjectEngineFactoryService : ILanguageService
    {
        public abstract IProjectEngineFactory FindFactory(ProjectSnapshot project);

        public abstract IProjectEngineFactory FindSerializableFactory(ProjectSnapshot project);

        public abstract RazorProjectEngine Create(ProjectSnapshot project, Action<RazorProjectEngineBuilder> configure);

        public abstract RazorProjectEngine Create(ProjectSnapshot project, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure);

        public abstract RazorProjectEngine Create(string directoryPath, Action<RazorProjectEngineBuilder> configure);
    }
}