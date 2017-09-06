// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class MvcExtensibilityConfiguration : ProjectExtensibilityConfiguration
    {
        public MvcExtensibilityConfiguration(
            ProjectExtensibilityConfigurationKind kind,
            ProjectExtensibilityAssembly razorAssembly, 
            ProjectExtensibilityAssembly mvcAssembly)
        {
            if (razorAssembly == null)
            {
                throw new ArgumentNullException(nameof(razorAssembly));
            }

            if (mvcAssembly == null)
            {
                throw new ArgumentNullException(nameof(mvcAssembly));
            }

            Kind = kind;
            RazorAssembly = razorAssembly;
            MvcAssembly = mvcAssembly;

            Assemblies = new[] { RazorAssembly, MvcAssembly, };
        }

        public override IReadOnlyList<ProjectExtensibilityAssembly> Assemblies { get; }

        public override ProjectExtensibilityConfigurationKind Kind { get; }

        public override ProjectExtensibilityAssembly RazorAssembly { get; }

        public ProjectExtensibilityAssembly MvcAssembly { get; }
    }
}
