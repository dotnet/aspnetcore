// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectExtensibilityConfiguration
    {
        public abstract IReadOnlyList<ProjectExtensibilityAssembly> Assemblies { get; }

        public abstract ProjectExtensibilityConfigurationKind Kind { get; }

        public abstract ProjectExtensibilityAssembly RazorAssembly { get; }
    }
}
