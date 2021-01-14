// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    /// <summary>
    /// Resolves assemblies that reference one of the specified "targetAssemblies" either directly or transitively.
    /// </summary>
    public class ReferenceResolver
    {
        private readonly HashSet<string> _mvcAssemblies;
        private readonly IReadOnlyList<AssemblyItem> _assemblyItems;
        private readonly Dictionary<AssemblyItem, Classification> _classifications;

        public ReferenceResolver(IReadOnlyList<string> targetAssemblies, IReadOnlyList<AssemblyItem> assemblyItems)
        {
            _mvcAssemblies = new HashSet<string>(targetAssemblies, StringComparer.Ordinal);
            _assemblyItems = assemblyItems;
            _classifications = new Dictionary<AssemblyItem, Classification>();

            Lookup = new Dictionary<string, AssemblyItem>(StringComparer.Ordinal);
            foreach (var item in assemblyItems)
            {
                Lookup[item.AssemblyName] = item;
            }
        }

        protected Dictionary<string, AssemblyItem> Lookup { get; }

        public IReadOnlyList<string> ResolveAssemblies()
        {
            var applicationParts = new List<string>();

            foreach (var item in _assemblyItems)
            {
                var classification = Resolve(item);
                if (classification == Classification.ReferencesMvc)
                {
                    applicationParts.Add(item.AssemblyName);
                }
            }

            return applicationParts;
        }

        private Classification Resolve(AssemblyItem assemblyItem)
        {
            if (_classifications.TryGetValue(assemblyItem, out var classification))
            {
                return classification;
            }

            // Initialize the dictionary with a value to short-circuit recursive references.
            classification = Classification.Unknown;
            _classifications[assemblyItem] = classification;

            if (assemblyItem.Path == null)
            {
                // We encountered a dependency that isn't part of this assembly's dependency set. We'll see if it happens to be an MVC assembly
                // since that's the only useful determination we can make given the assembly name.
                classification = _mvcAssemblies.Contains(assemblyItem.AssemblyName) ?
                    Classification.IsMvc :
                    Classification.DoesNotReferenceMvc;
            }
            else if (assemblyItem.IsFrameworkReference)
            {
                // We do not allow transitive references to MVC via a framework reference to count.
                // e.g. depending on Microsoft.AspNetCore.SomeThingNewThatDependsOnMvc would not result in an assembly being treated as
                // referencing MVC.
                classification = _mvcAssemblies.Contains(assemblyItem.AssemblyName) ?
                    Classification.IsMvc :
                    Classification.DoesNotReferenceMvc;
            }
            else if (_mvcAssemblies.Contains(assemblyItem.AssemblyName))
            {
                classification = Classification.IsMvc;
            }
            else
            {
                classification = Classification.DoesNotReferenceMvc;
                foreach (var reference in GetReferences(assemblyItem.Path))
                {
                    var referenceClassification = Resolve(reference);

                    if (referenceClassification == Classification.IsMvc || referenceClassification == Classification.ReferencesMvc)
                    {
                        classification = Classification.ReferencesMvc;
                        break;
                    }
                }
            }

            Debug.Assert(classification != Classification.Unknown);
            _classifications[assemblyItem] = classification;
            return classification;
        }

        protected virtual IReadOnlyList<AssemblyItem> GetReferences(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    throw new ReferenceAssemblyNotFoundException(file);
                }

                using var peReader = new PEReader(File.OpenRead(file));
                if (!peReader.HasMetadata)
                {
                    return Array.Empty<AssemblyItem>(); // not a managed assembly
                }

                var metadataReader = peReader.GetMetadataReader();

                var references = new List<AssemblyItem>(metadataReader.AssemblyReferences.Count);
                foreach (var handle in metadataReader.AssemblyReferences)
                {
                    var reference = metadataReader.GetAssemblyReference(handle);
                    var referenceName = metadataReader.GetString(reference.Name);

                    if (!Lookup.TryGetValue(referenceName, out var assemblyItem))
                    {
                        // A dependency references an item that isn't referenced by this project.
                        // We'll construct an item for so that we can calculate the classification based on it's name.
                        assemblyItem = new AssemblyItem
                        {
                            AssemblyName = referenceName,
                        };

                        Lookup[referenceName] = assemblyItem;
                    }

                    references.Add(assemblyItem);
                }

                return references;
            }
            catch (BadImageFormatException)
            {
                // not a PE file, or invalid metadata
            }

            return Array.Empty<AssemblyItem>(); // not a managed assembly
        }

        protected enum Classification
        {
            Unknown,
            DoesNotReferenceMvc,
            ReferencesMvc,
            IsMvc,
        }
    }
}
