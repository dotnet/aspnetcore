// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class MvcExtensibilityConfiguration : ProjectExtensibilityConfiguration
    {
        public MvcExtensibilityConfiguration(
            RazorLanguageVersion languageVersion,
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
            LanguageVersion = languageVersion;

            Assemblies = new[] { RazorAssembly, MvcAssembly, };
        }

        public override IReadOnlyList<ProjectExtensibilityAssembly> Assemblies { get; }

        // MVC: '2.0.0' (fallback) | Razor Language '2.0.0'
        // or 
        // MVC: '2.1.3' | Razor Language '2.1.3'
        public override string DisplayName => $"MVC: {MvcAssembly.Identity.Version.ToString(3)}" + (Kind == ProjectExtensibilityConfigurationKind.Fallback? " (fallback)" : string.Empty) + " | " + LanguageVersion;

        public override ProjectExtensibilityConfigurationKind Kind { get; }

        public override ProjectExtensibilityAssembly RazorAssembly { get; }

        public override RazorLanguageVersion LanguageVersion { get; }

        public ProjectExtensibilityAssembly MvcAssembly { get; }

        public override bool Equals(ProjectExtensibilityConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            // We're intentionally ignoring the 'Kind' here. That's mostly for diagnostics and doesn't influence any behavior.
            return LanguageVersion == other.LanguageVersion &&
                Enumerable.SequenceEqual(
                    Assemblies.OrderBy(a => a.Identity.Name).Select(a => a.Identity),
                    other.Assemblies.OrderBy(a => a.Identity.Name).Select(a => a.Identity),
                    AssemblyIdentityEqualityComparer.NameAndVersion);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            foreach (var assembly in Assemblies.OrderBy(a => a.Identity.Name))
            {
                hash.Add(assembly);
            }

            hash.Add(LanguageVersion);

            return hash;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
