// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectExtensibilityConfiguration : IEquatable<ProjectExtensibilityConfiguration>
    {
        public abstract IReadOnlyList<ProjectExtensibilityAssembly> Assemblies { get; }

        public abstract string DisplayName { get; }

        public abstract ProjectExtensibilityConfigurationKind Kind { get; }

        public abstract ProjectExtensibilityAssembly RazorAssembly { get; }

        public abstract RazorLanguageVersion LanguageVersion { get; }

        public abstract bool Equals(ProjectExtensibilityConfiguration other);

        public abstract override int GetHashCode();

        public override bool Equals(object obj)
        {
            return Equals(obj as ProjectExtensibilityConfiguration);
        }
    }
}
