// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using static Microsoft.VisualStudio.LanguageServices.Razor.ReflectionNames;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(IRazorEngineAssemblyResolver))]
    internal class DefaultRazorEngineAssemblyResolver : IRazorEngineAssemblyResolver
    {
        public async Task<IEnumerable<RazorEngineAssembly>> GetRazorEngineAssembliesAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            var assemblies = GetRazorCustomizationAssemblies(compilation);

            if (assemblies.Count == 0)
            {
                Console.WriteLine();
            }

            return assemblies;
        }

        private static List<RazorEngineAssembly> GetRazorCustomizationAssemblies(Compilation compilation)
        {
            // The goal here is to find the set of assemblies + paths that have some kind of
            // Razor extensibility.
            //
            // We do that by making a compilation and then looking through the set of assembly names
            // (AssemblyIdentity) and references to dlls on disk (PortableExecutableReference) to find
            // uses of RazorEngineCustomizationAttribute and RazorEngineDependencyAttribute.
            //
            // We're limited to supporting files on disk because we will need to shadow copy them
            // and manually load them.
            //
            // Also note that we're not doing anything here to explicitly uniquify this list, since
            // we're limiting the set of candidates to the set of assemblies used for compilation, which
            // has already been processed by Roslyn.
            var results = new List<RazorEngineAssembly>();

            // The RazorEngineDependencyAttribute also allows specifying an assembly name to go along
            // with a piece of extensibility. We'll collect these on the first pass through the assemblies
            // and then look those up by name.
            var unresolvedIdentities = new List<AssemblyIdentity>();

            foreach (var reference in compilation.References)
            {
                var peReference = reference as PortableExecutableReference;
                if (peReference == null || peReference.FilePath == null)
                {
                    // No path, can't load it.
                    continue;
                }

                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol == null)
                {
                    // It's unlikely, but possible that a reference might be a module instead of an assembly. 
                    // We can't load that, so just skip it.
                    continue;
                }

                var identity = assemblySymbol.Identity;
                if (identity.Name == RazorAssemblyName)
                {
                    // This is the main Razor assembly.
                    results.Add(new RazorEngineAssembly(identity, peReference.FilePath));
                }

                // Now we're looking for the Razor exensibility attributes.
                var attributes = assemblySymbol.GetAttributes();
                for (var i = 0; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    var name = attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (string.Equals(
                        CustomizationAttribute,
                        name,
                        StringComparison.Ordinal))
                    {
                        results.Add(new RazorEngineAssembly(identity, peReference.FilePath));
                    }

                    if (string.Equals(
                        DependencyAttribute,
                        name,
                        StringComparison.Ordinal))
                    {
                        // This attribute refers to a separate assembly for which we will need to resolve a path.
                        //
                        // Ignore parsing failures here.
                        AssemblyIdentity dependency;
                        if (AssemblyIdentity.TryParseDisplayName((string)attribute.ConstructorArguments[0].Value, out dependency))
                        {
                            unresolvedIdentities.Add(dependency);
                        }
                    }
                }

            }

            // Now we need to do another pass to resolve all the unresolved names.
            if (unresolvedIdentities.Count > 0)
            {
                //while (identities.MoveNext() && references.MoveNext())
                //{
                //    var peReference = references.Current as PortableExecutableReference;
                //    if (peReference == null || peReference.FilePath == null)
                //    {
                //        // No path, can't load it.
                //        continue;
                //    }

                //    var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(peReference) as IAssemblySymbol;
                //    if (assemblySymbol == null)
                //    {
                //        // It's unlikely, but possible that a reference might be a module instead of an assembly. 
                //        // We can't load that, so just skip it.
                //        continue;
                //    }

                //    for (var i = 0; i < unresolvedIdentities.Count; i++)
                //    {
                //        // Note: argument ordering here is significant. We expect that the attribute will often refer to a
                //        // partial name and omit details like the version and public-key, therefore the value from the
                //        // attribute must be the first argument.
                //        if (AssemblyIdentityComparer.Default.ReferenceMatchesDefinition(
                //            unresolvedIdentities[i],
                //            identities.Current))
                //        {
                //            results.Add(new RazorEngineAssembly(identities.Current, peReference.FilePath));
                //            break;
                //        }
                //    }
                //}
            }

            return results;
        }
    }
}
