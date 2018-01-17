// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    // Discovers assemblies that are part of the MVC application using the DependencyContext.
    public static class DefaultAssemblyPartDiscoveryProvider
    {
        private static readonly string PrecompiledViewsAssemblySuffix = ".PrecompiledViews";
        private static readonly IReadOnlyList<string> ViewAssemblySuffixes = new string[]
        {
            PrecompiledViewsAssemblySuffix,
            ".Views",
        };

        private const string AdditionalReferenceKey = "Microsoft.AspNetCore.Mvc.AdditionalReference";
        private static readonly char[] MetadataSeparators = new[] { ',' };

        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.AspNetCore.All",
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Mvc.Abstractions",
            "Microsoft.AspNetCore.Mvc.ApiExplorer",
            "Microsoft.AspNetCore.Mvc.Core",
            "Microsoft.AspNetCore.Mvc.Cors",
            "Microsoft.AspNetCore.Mvc.DataAnnotations",
            "Microsoft.AspNetCore.Mvc.Formatters.Json",
            "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            "Microsoft.AspNetCore.Mvc.Localization",
            "Microsoft.AspNetCore.Mvc.Razor",
            "Microsoft.AspNetCore.Mvc.Razor.Extensions",
            "Microsoft.AspNetCore.Mvc.RazorPages",
            "Microsoft.AspNetCore.Mvc.TagHelpers",
            "Microsoft.AspNetCore.Mvc.ViewFeatures"
        };

        // For testing purposes only.
        internal static Func<string, Assembly> AssemblyLoader { get; set; } = Assembly.LoadFile;
        internal static Func<string, bool> AssemblyResolver { get; set; } = File.Exists;

        public static IEnumerable<ApplicationPart> DiscoverAssemblyParts(string entryPointAssemblyName)
        {
            // We need to produce a stable order of the parts that is given by:
            // 1) Parts that are not additional parts go before parts that are additional parts.
            // 2) The entry point part goes before any other part in the system.
            // 3) The entry point additional parts go before any other additional parts.
            // 4) Parts are finally ordered by their full name to produce a stable ordering.
            var entryAssembly = Assembly.Load(new AssemblyName(entryPointAssemblyName));
            var context = DependencyContext.Load(entryAssembly);

            var candidateAssemblies = new SortedSet<Assembly>(
                GetCandidateAssemblies(entryAssembly, context),
                FullNameAssemblyComparer.Instance);

            var (additionalReferences, entryAssemblyAdditionalReferences) = ResolveAdditionalReferences(
                entryAssembly,
                candidateAssemblies);

            candidateAssemblies.Remove(entryAssembly);
            candidateAssemblies.ExceptWith(additionalReferences);
            candidateAssemblies.ExceptWith(entryAssemblyAdditionalReferences);

            // Create the list of assembly parts.
            return CreateParts();

            IEnumerable<AssemblyPart> CreateParts()
            {
                yield return new AssemblyPart(entryAssembly);
                foreach (var candidateAssembly in candidateAssemblies)
                {
                    yield return new AssemblyPart(candidateAssembly);
                }
                foreach (var entryAdditionalAssembly in entryAssemblyAdditionalReferences)
                {
                    yield return new AdditionalAssemblyPart(entryAdditionalAssembly);
                }
                foreach (var additionalAssembly in additionalReferences)
                {
                    yield return new AdditionalAssemblyPart(additionalAssembly);
                }
            }
        }

        internal static AdditionalReferencesPair ResolveAdditionalReferences(
            Assembly entryAssembly,
            SortedSet<Assembly> candidateAssemblies)
        {
            var additionalAssemblyReferences = candidateAssemblies
                .Select(ca =>
                    (assembly: ca,
                     metadata: ca.GetCustomAttributes<AssemblyMetadataAttribute>()
                        .Where(ama => ama.Key.Equals(AdditionalReferenceKey, StringComparison.Ordinal)).ToArray()));

            // Find out all the additional references defined by the assembly.
            // [assembly: AssemblyMetadataAttribute("Microsoft.AspNetCore.Mvc.AdditionalReference", "Library.PrecompiledViews.dll,true|false")]
            var additionalReferences = new SortedSet<Assembly>(FullNameAssemblyComparer.Instance);
            var entryAssemblyAdditionalReferences = new SortedSet<Assembly>(FullNameAssemblyComparer.Instance);
            foreach (var (assembly, metadata) in additionalAssemblyReferences)
            {
                if (metadata.Length > 0)
                {
                    foreach (var metadataAttribute in metadata)
                    {
                        AddAdditionalReference(
                            LoadFromMetadata(assembly, metadataAttribute),
                            entryAssembly,
                            assembly,
                            additionalReferences,
                            entryAssemblyAdditionalReferences);
                    }
                }
                else
                {
                    // Fallback to loading the views like in previous versions if the additional reference metadata
                    // attribute is not present.
                    AddAdditionalReference(
                        LoadFromConvention(assembly),
                        entryAssembly,
                        assembly,
                        additionalReferences,
                        entryAssemblyAdditionalReferences);
                }
            }

            return new AdditionalReferencesPair
            {
                AdditionalAssemblies = additionalReferences,
                EntryAssemblyAdditionalAssemblies = entryAssemblyAdditionalReferences
            };
        }

        private static Assembly LoadFromMetadata(Assembly assembly, AssemblyMetadataAttribute metadataAttribute)
        {
            var (metadataPath, metadataIncludeByDefault) = ParseMetadataAttribute(metadataAttribute);
            if (metadataPath == null ||
                metadataIncludeByDefault == null ||
                !string.Equals(metadataIncludeByDefault, "true", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var fileName = Path.GetFileName(metadataPath);
            var filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), fileName);
            var additionalAssembly = LoadAssembly(filePath);

            if (additionalAssembly == null)
            {
                return null;
            }

            return additionalAssembly;
        }

        private static (string metadataPath, string metadataIncludeByDefault) ParseMetadataAttribute(
            AssemblyMetadataAttribute metadataAttribute)
        {
            var data = metadataAttribute.Value.Split(MetadataSeparators);
            if (data.Length != 2 || string.IsNullOrWhiteSpace(data[0]) || string.IsNullOrWhiteSpace(data[1]))
            {
                return default;
            }

            return (data[0], data[1]);
        }

        private static Assembly LoadAssembly(string filePath)
        {
            Assembly viewsAssembly = null;
            if (AssemblyResolver(filePath))
            {
                try
                {
                    viewsAssembly = AssemblyLoader(filePath);
                }
                catch (FileLoadException)
                {
                    // Don't throw if assembly cannot be loaded. This can happen if the file is not a managed assembly.
                }
            }

            return viewsAssembly;
        }

        private static Assembly LoadFromConvention(Assembly assembly)
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
            {
                return null;
            }

            for (var i = 0; i < ViewAssemblySuffixes.Count; i++)
            {
                var fileName = assembly.GetName().Name + ViewAssemblySuffixes[i] + ".dll";
                var filePath = Path.Combine(Path.GetDirectoryName(assembly.Location), fileName);

                var viewsAssembly = LoadAssembly(filePath);
                if (viewsAssembly != null)
                {
                    return viewsAssembly;
                }
            }

            return null;
        }

        private static void AddAdditionalReference(
            Assembly additionalReference,
            Assembly entryAssembly,
            Assembly assembly,
            SortedSet<Assembly> additionalReferences,
            SortedSet<Assembly> entryAssemblyAdditionalReferences)
        {
            if (additionalReference == null ||
                additionalReferences.Contains(additionalReference) ||
                entryAssemblyAdditionalReferences.Contains(additionalReference))
            {
                return;
            }

            if (assembly.Equals(entryAssembly))
            {
                entryAssemblyAdditionalReferences.Add(additionalReference);
            }
            else
            {
                additionalReferences.Add(additionalReference);
            }
        }

        internal class AdditionalReferencesPair
        {
            public SortedSet<Assembly> AdditionalAssemblies { get; set; }
            public SortedSet<Assembly> EntryAssemblyAdditionalAssemblies { get; set; }

            public void Deconstruct(
                out SortedSet<Assembly> additionalAssemblies,
                out SortedSet<Assembly> entryAssemblyAdditionalAssemblies)
            {
                additionalAssemblies = AdditionalAssemblies;
                entryAssemblyAdditionalAssemblies = EntryAssemblyAdditionalAssemblies;
            }
        }

        internal class FullNameAssemblyComparer : IComparer<Assembly>
        {
            public static IComparer<Assembly> Instance { get; } = new FullNameAssemblyComparer();

            public int Compare(Assembly x, Assembly y) =>
                string.Compare(x?.FullName, y?.FullName, StringComparison.Ordinal);
        }

        internal static IEnumerable<Assembly> GetCandidateAssemblies(Assembly entryAssembly, DependencyContext dependencyContext)
        {
            if (dependencyContext == null)
            {
                // Use the entry assembly as the sole candidate.
                return new[] { entryAssembly };
            }

            return GetCandidateLibraries(dependencyContext)
                .SelectMany(library => library.GetDefaultAssemblyNames(dependencyContext))
                .Select(Assembly.Load);
        }

        // Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.
        // By default it returns all assemblies that reference any of the primary MVC assemblies
        // while ignoring MVC assemblies.
        // Internal for unit testing
        internal static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext)
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            var candidatesResolver = new CandidateResolver(dependencyContext.RuntimeLibraries, ReferenceAssemblies);
            return candidatesResolver.GetCandidates();
        }

        private class CandidateResolver
        {
            private readonly IDictionary<string, Dependency> _runtimeDependencies;

            public CandidateResolver(IReadOnlyList<RuntimeLibrary> runtimeDependencies, ISet<string> referenceAssemblies)
            {
                var dependenciesWithNoDuplicates = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);
                foreach (var dependency in runtimeDependencies)
                {
                    if (dependenciesWithNoDuplicates.ContainsKey(dependency.Name))
                    {
                        throw new InvalidOperationException(Resources.FormatCandidateResolver_DifferentCasedReference(dependency.Name));
                    }
                    dependenciesWithNoDuplicates.Add(dependency.Name, CreateDependency(dependency, referenceAssemblies));
                }

                _runtimeDependencies = dependenciesWithNoDuplicates;
            }

            private Dependency CreateDependency(RuntimeLibrary library, ISet<string> referenceAssemblies)
            {
                var classification = DependencyClassification.Unknown;
                if (referenceAssemblies.Contains(library.Name))
                {
                    classification = DependencyClassification.MvcReference;
                }

                return new Dependency(library, classification);
            }

            private DependencyClassification ComputeClassification(string dependency)
            {
                if (!_runtimeDependencies.ContainsKey(dependency))
                {
                    // Library does not have runtime dependency. Since we can't infer
                    // anything about it's references, we'll assume it does not have a reference to Mvc.
                    return DependencyClassification.DoesNotReferenceMvc;
                }

                var candidateEntry = _runtimeDependencies[dependency];
                if (candidateEntry.Classification != DependencyClassification.Unknown)
                {
                    return candidateEntry.Classification;
                }
                else
                {
                    var classification = DependencyClassification.DoesNotReferenceMvc;
                    foreach (var candidateDependency in candidateEntry.Library.Dependencies)
                    {
                        var dependencyClassification = ComputeClassification(candidateDependency.Name);
                        if (dependencyClassification == DependencyClassification.ReferencesMvc ||
                            dependencyClassification == DependencyClassification.MvcReference)
                        {
                            classification = DependencyClassification.ReferencesMvc;
                            break;
                        }
                    }

                    candidateEntry.Classification = classification;

                    return classification;
                }
            }

            public IEnumerable<RuntimeLibrary> GetCandidates()
            {
                foreach (var dependency in _runtimeDependencies)
                {
                    if (ComputeClassification(dependency.Key) == DependencyClassification.ReferencesMvc)
                    {
                        yield return dependency.Value.Library;
                    }
                }
            }

            private class Dependency
            {
                public Dependency(RuntimeLibrary library, DependencyClassification classification)
                {
                    Library = library;
                    Classification = classification;
                }

                public RuntimeLibrary Library { get; }

                public DependencyClassification Classification { get; set; }

                public override string ToString()
                {
                    return $"Library: {Library.Name}, Classification: {Classification}";
                }
            }

            private enum DependencyClassification
            {
                Unknown = 0,

                /// <summary>
                /// References (directly or transitively) one of the Mvc packages listed in
                /// <see cref="ReferenceAssemblies"/>.
                /// </summary>
                ReferencesMvc = 1,

                /// <summary>
                /// Does not reference (directly or transitively) one of the Mvc packages listed by
                /// <see cref="ReferenceAssemblies"/>.
                /// </summary>
                DoesNotReferenceMvc = 2,

                /// <summary>
                /// One of the references listed in <see cref="ReferenceAssemblies"/>.
                /// </summary>
                MvcReference = 3,
            }
        }
    }
}
