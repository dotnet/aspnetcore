// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // This is hardcoded for now. A more complete design would fan out to a list of providers.
    internal class DefaultProjectExtensibilityConfigurationFactory : ProjectExtensibilityConfigurationFactory
    {
        private const string MvcAssemblyName = "Microsoft.AspNetCore.Mvc.Razor";
        private const string RazorV1AssemblyName = "Microsoft.AspNetCore.Razor";
        private const string RazorV2AssemblyName = "Microsoft.AspNetCore.Razor.Language";

        // Using MaxValue here so that we ignore patch and build numbers. We only want to compare major/minor.
        private static readonly Version MaxSupportedRazorVersion = new Version(2, 0, Int32.MaxValue, Int32.MaxValue);
        private static readonly Version MaxSupportedMvcVersion = new Version(2, 0, Int32.MaxValue, Int32.MaxValue);

        private static readonly Version DefaultRazorVersion = new Version(2, 0, 0, 0);
        private static readonly Version DefaultMvcVersion = new Version(2, 0, 0, 0);

        public async override Task<ProjectExtensibilityConfiguration> GetConfigurationAsync(Project project, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var compilation = await project.GetCompilationAsync(cancellationToken);
            return GetConfiguration(compilation.ReferencedAssemblyNames);
        }

        // internal/separate for testing.
        internal ProjectExtensibilityConfiguration GetConfiguration(IEnumerable<AssemblyIdentity> references)
        {
            // Avoiding ToDictionary here because we don't want a crash if there is a duplicate name.
            var assemblies = new Dictionary<string, AssemblyIdentity>();
            foreach (var assembly in references)
            {
                assemblies[assembly.Name] = assembly;
            }

            // First we look for the V2+ Razor Assembly. If we find this then its version is the correct Razor version.
            AssemblyIdentity razorAssembly;
            if (assemblies.TryGetValue(RazorV2AssemblyName, out razorAssembly))
            {
                if (razorAssembly.Version == null || razorAssembly.Version > MaxSupportedRazorVersion)
                {
                    // This is a newer Razor version than we know, treat it as a fallback case.
                    razorAssembly = null;
                }
            }
            else if (assemblies.TryGetValue(RazorV1AssemblyName, out razorAssembly))
            {
                // This assembly only counts as the 'Razor' assembly if it's a version lower than 2.0.0.
                if (razorAssembly.Version == null || razorAssembly.Version >= new Version(2, 0, 0, 0))
                {
                    razorAssembly = null;
                }
            }

            AssemblyIdentity mvcAssembly;
            if (assemblies.TryGetValue(MvcAssemblyName, out mvcAssembly))
            {
                if (mvcAssembly.Version == null || mvcAssembly.Version > MaxSupportedMvcVersion)
                {
                    // This is a newer MVC version than we know, treat it as a fallback case.
                    mvcAssembly = null;
                }
            }

            RazorLanguageVersion languageVersion = null;
            if (razorAssembly != null && mvcAssembly != null)
            {
                languageVersion = GetLanguageVersion(razorAssembly);

                // This means we've definitely found a supported Razor version and an MVC version.
                return new MvcExtensibilityConfiguration(
                    languageVersion,
                    ProjectExtensibilityConfigurationKind.ApproximateMatch,
                    new ProjectExtensibilityAssembly(razorAssembly),
                    new ProjectExtensibilityAssembly(mvcAssembly));
            }

            // If we get here it means we didn't find everything, so we have to guess.
            if (razorAssembly == null || razorAssembly.Version == null)
            {
                razorAssembly = new AssemblyIdentity(RazorV2AssemblyName, DefaultRazorVersion);
            }

            if (mvcAssembly == null || mvcAssembly.Version == null)
            {
                mvcAssembly = new AssemblyIdentity(MvcAssemblyName, DefaultMvcVersion);
            }

            if (languageVersion == null)
            {
                languageVersion = GetLanguageVersion(razorAssembly);
            }

            return new MvcExtensibilityConfiguration(
                languageVersion,
                ProjectExtensibilityConfigurationKind.Fallback,
                new ProjectExtensibilityAssembly(razorAssembly),
                new ProjectExtensibilityAssembly(mvcAssembly));
        }

        // Internal for testing
        internal static RazorLanguageVersion GetLanguageVersion(AssemblyIdentity razorAssembly)
        {
            // This is inferred from the assembly for now, the Razor language version will eventually flow from MSBuild.

            var razorAssemblyVersion = razorAssembly.Version;
            if (razorAssemblyVersion.Major == 1)
            {
                if (razorAssemblyVersion.Minor >= 1)
                {
                    return RazorLanguageVersion.Version_1_1;
                }

                return RazorLanguageVersion.Version_1_0;
            }

            if (razorAssemblyVersion.Major == 2)
            {
                if (razorAssemblyVersion.Minor >= 1)
                {
                    return RazorLanguageVersion.Version_2_1;
                }

                return RazorLanguageVersion.Version_2_0;
            }

            // Couldn't determine version based off of assembly, fallback to latest.
            return RazorLanguageVersion.Latest;
        }
    }
}
