// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal class DependencyContextWriter
    {
        public void Write(DependencyContext context, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
                {
                    Write(context).WriteTo(jsonWriter);
                }
            }
        }

        private JObject Write(DependencyContext context)
        {
            var contextObject =  new JObject(
                new JProperty(DependencyContextStrings.RuntimeTargetPropertyName, WriteRuntimeTargetInfo(context)),
                new JProperty(DependencyContextStrings.CompilationOptionsPropertName, WriteCompilationOptions(context.CompilationOptions)),
                new JProperty(DependencyContextStrings.TargetsPropertyName, WriteTargets(context)),
                new JProperty(DependencyContextStrings.LibrariesPropertyName, WriteLibraries(context))
                );
            if (context.RuntimeGraph.Any())
            {
                contextObject.Add(new JProperty(DependencyContextStrings.RuntimesPropertyName, WriteRuntimeGraph(context)));
            }
            return contextObject;
        }

        private string WriteRuntimeTargetInfo(DependencyContext context)
        {
            return context.IsPortable?
                context.TargetFramework :
                context.TargetFramework + DependencyContextStrings.VersionSeperator + context.Runtime;
        }

        private JObject WriteRuntimeGraph(DependencyContext context)
        {
            return new JObject(
                context.RuntimeGraph.Select(g => new JProperty(g.Runtime, new JArray(g.Fallbacks)))
                );
        }

        private JObject WriteCompilationOptions(CompilationOptions compilationOptions)
        {
            var o = new JObject();
            if (compilationOptions.Defines?.Any() == true)
            {
                o[DependencyContextStrings.DefinesPropertyName] = new JArray(compilationOptions.Defines);
            }
            AddPropertyIfNotNull(o, DependencyContextStrings.LanguageVersionPropertyName, compilationOptions.LanguageVersion);
            AddPropertyIfNotNull(o, DependencyContextStrings.PlatformPropertyName, compilationOptions.Platform);
            AddPropertyIfNotNull(o, DependencyContextStrings.AllowUnsafePropertyName, compilationOptions.AllowUnsafe);
            AddPropertyIfNotNull(o, DependencyContextStrings.WarningsAsErrorsPropertyName, compilationOptions.WarningsAsErrors);
            AddPropertyIfNotNull(o, DependencyContextStrings.OptimizePropertyName, compilationOptions.Optimize);
            AddPropertyIfNotNull(o, DependencyContextStrings.KeyFilePropertyName, compilationOptions.KeyFile);
            AddPropertyIfNotNull(o, DependencyContextStrings.DelaySignPropertyName, compilationOptions.DelaySign);
            AddPropertyIfNotNull(o, DependencyContextStrings.PublicSignPropertyName, compilationOptions.PublicSign);
            AddPropertyIfNotNull(o, DependencyContextStrings.EmitEntryPointPropertyName, compilationOptions.EmitEntryPoint);
            AddPropertyIfNotNull(o, DependencyContextStrings.GenerateXmlDocumentationPropertyName, compilationOptions.GenerateXmlDocumentation);
            AddPropertyIfNotNull(o, DependencyContextStrings.DebugTypePropertyName, compilationOptions.DebugType);
            return o;
        }

        private void AddPropertyIfNotNull<T>(JObject o, string name, T value)
            {
            if (value != null)
            {
                o.Add(new JProperty(name, value));
            }
            }

        private JObject WriteTargets(DependencyContext context)
        {
            if (context.IsPortable)
            {
                return new JObject(
                    new JProperty(context.TargetFramework, WritePortableTarget(context.RuntimeLibraries, context.CompileLibraries))
                    );
            }

            return new JObject(
                new JProperty(context.TargetFramework, WriteTarget(context.CompileLibraries)),
                new JProperty(context.TargetFramework + DependencyContextStrings.VersionSeperator + context.Runtime,
                    WriteTarget(context.RuntimeLibraries))
                );
        }

        private JObject WriteTarget(IReadOnlyList<Library> libraries)
        {
            return new JObject(
                libraries.Select(library =>
                    new JProperty(library.Name + DependencyContextStrings.VersionSeperator + library.Version, WriteTargetLibrary(library))));
        }

        private JObject WritePortableTarget(IReadOnlyList<RuntimeLibrary> runtimeLibraries, IReadOnlyList<CompilationLibrary> compilationLibraries)
        {
            var runtimeLookup = runtimeLibraries.ToDictionary(l => l.Name);
            var compileLookup = compilationLibraries.ToDictionary(l => l.Name);

            var targetObject = new JObject();

            foreach (var packageName in runtimeLookup.Keys.Concat(compileLookup.Keys).Distinct())
            {
                RuntimeLibrary runtimeLibrary;
                runtimeLookup.TryGetValue(packageName, out runtimeLibrary);

                CompilationLibrary compilationLibrary;
                compileLookup.TryGetValue(packageName, out compilationLibrary);

                if (compilationLibrary != null && runtimeLibrary != null)
                {
                    Debug.Assert(compilationLibrary.Serviceable == runtimeLibrary.Serviceable);
                    Debug.Assert(compilationLibrary.Version == runtimeLibrary.Version);
                    Debug.Assert(compilationLibrary.Hash == runtimeLibrary.Hash);
                    Debug.Assert(compilationLibrary.Type == runtimeLibrary.Type);
                }

                var library = (Library)compilationLibrary ?? (Library)runtimeLibrary;
                targetObject.Add(
                    new JProperty(library.Name + DependencyContextStrings.VersionSeperator + library.Version,
                        WritePortableTargetLibrary(runtimeLibrary, compilationLibrary)
                        )
                    );

            }
            return targetObject;
        }

        private void AddCompilationAssemblies(JObject libraryObject, IEnumerable<string> compilationAssemblies)
        {
            if (!compilationAssemblies.Any())
            {
                return;
            }
            libraryObject.Add(new JProperty(DependencyContextStrings.CompileTimeAssembliesKey,
                 WriteAssetList(compilationAssemblies))
             );
        }

        private void AddRuntimeAssemblies(JObject libraryObject, IEnumerable<RuntimeAssembly> runtimeAssemblies)
        {
            if (!runtimeAssemblies.Any())
            {
                return;
            }
            libraryObject.Add(new JProperty(DependencyContextStrings.RuntimeAssembliesKey,
                       WriteAssetList(runtimeAssemblies.Select(a => a.Path)))
                   );
        }

        private void AddDependencies(JObject libraryObject, IEnumerable<Dependency> dependencies)
        {
            if (!dependencies.Any())
            {
                return;
            }
            libraryObject.AddFirst(
                new JProperty(DependencyContextStrings.DependenciesPropertyName,
                new JObject(
                    dependencies.Select(dependency => new JProperty(dependency.Name, dependency.Version))))
                    );
        }

        private void AddResourceAssemblies(JObject libraryObject, IEnumerable<ResourceAssembly> resourceAssemblies)
        {
            if (!resourceAssemblies.Any())
            {
                return;
            }
            libraryObject.Add(DependencyContextStrings.ResourceAssembliesPropertyName,
                new JObject(resourceAssemblies.Select(a =>
                    new JProperty(NormalizePath(a.Path), new JObject(new JProperty(DependencyContextStrings.LocalePropertyName, a.Locale))))
                    )
                );
        }

        private JObject WriteTargetLibrary(Library library)
        {
            var runtimeLibrary = library as RuntimeLibrary;
            if (runtimeLibrary != null)
            {
                var libraryObject = new JObject();
                AddDependencies(libraryObject, runtimeLibrary.Dependencies);
                AddRuntimeAssemblies(libraryObject, runtimeLibrary.Assemblies);
                AddResourceAssemblies(libraryObject, runtimeLibrary.ResourceAssemblies);

                if (runtimeLibrary.NativeLibraries.Any())
                {
                    libraryObject.Add(DependencyContextStrings.NativeLibrariesKey, WriteAssetList(runtimeLibrary.NativeLibraries));
                }

                return libraryObject;
            }

            var compilationLibrary = library as CompilationLibrary;
            if (compilationLibrary != null)
            {
                var libraryObject = new JObject();
                AddDependencies(libraryObject, compilationLibrary.Dependencies);
                AddCompilationAssemblies(libraryObject, compilationLibrary.Assemblies);
                return libraryObject;
            }
            throw new NotSupportedException();
        }

        private JObject WritePortableTargetLibrary(RuntimeLibrary runtimeLibrary, CompilationLibrary compilationLibrary)
        {
            var libraryObject = new JObject();

            var dependencies = new HashSet<Dependency>();
            if (runtimeLibrary != null)
            {

                if (runtimeLibrary.RuntimeTargets.Any())
                {
                    libraryObject.Add(new JProperty(
                        DependencyContextStrings.RuntimeTargetsPropertyName,
                        new JObject(runtimeLibrary.RuntimeTargets.SelectMany(WriteRuntimeTarget)))
                        );
                }
                AddRuntimeAssemblies(libraryObject, runtimeLibrary.Assemblies);
                AddResourceAssemblies(libraryObject, runtimeLibrary.ResourceAssemblies);
                libraryObject.Add(DependencyContextStrings.NativeLibrariesKey, WriteAssetList(runtimeLibrary.NativeLibraries));

                dependencies.UnionWith(runtimeLibrary.Dependencies);
            }

            if (compilationLibrary != null)
            {
                AddCompilationAssemblies(libraryObject, compilationLibrary.Assemblies);

                dependencies.UnionWith(compilationLibrary.Dependencies);
            }

            AddDependencies(libraryObject, dependencies);
            return libraryObject;
        }

        private IEnumerable<JProperty> WriteRuntimeTarget(RuntimeTarget target)
        {
            var runtime = WriteRuntimeTargetAssemblies(
                target.Assemblies.Select(a => a.Path),
                target.Runtime,
                DependencyContextStrings.RuntimeAssetType);

            var native = WriteRuntimeTargetAssemblies(
                target.NativeLibraries,
                target.Runtime,
                DependencyContextStrings.NativeAssetType);

            return runtime.Concat(native);
        }

        private IEnumerable<JProperty> WriteRuntimeTargetAssemblies(IEnumerable<string> assemblies, string runtime, string assetType)
        {
            foreach (var assembly in assemblies)
            {
                yield return new JProperty(NormalizePath(assembly),
                    new JObject(
                        new JProperty(DependencyContextStrings.RidPropertyName, runtime),
                        new JProperty(DependencyContextStrings.AssetTypePropertyName, assetType)
                        )
                    );
            }
        }

        private JObject WriteAssetList(IEnumerable<string> assetPaths)
        {
            return new JObject(assetPaths.Select(assembly => new JProperty(NormalizePath(assembly), new JObject())));
        }

        private JObject WriteLibraries(DependencyContext context)
        {
            var allLibraries =
                context.RuntimeLibraries.Cast<Library>().Concat(context.CompileLibraries)
                    .GroupBy(library => library.Name + DependencyContextStrings.VersionSeperator + library.Version);

            return new JObject(allLibraries.Select(libraries=> new JProperty(libraries.Key, WriteLibrary(libraries.First()))));
        }

        private JObject WriteLibrary(Library library)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.TypePropertyName, library.Type),
                new JProperty(DependencyContextStrings.ServiceablePropertyName, library.Serviceable),
                new JProperty(DependencyContextStrings.Sha512PropertyName, library.Hash)
                );
        }

        private string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
