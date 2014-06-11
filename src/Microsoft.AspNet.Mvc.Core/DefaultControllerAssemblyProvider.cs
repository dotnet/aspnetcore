// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultControllerAssemblyProvider : IControllerAssemblyProvider
    {
        // List of Mvc assemblies that we'll use as roots for controller discovery.
        private static readonly HashSet<string> _mvcAssemblyList = new HashSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Core",
            "Microsoft.AspNet.Mvc.ModelBinding",
            "Microsoft.AspNet.Mvc.Razor",
            "Microsoft.AspNet.Mvc.Razor.Host",
            "Microsoft.AspNet.Mvc.Rendering",
        };

        private readonly ILibraryManager _libraryManager;

        public DefaultControllerAssemblyProvider(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                return GetCandidateLibraries().Select(Load);
            }
        }

        internal IEnumerable<ILibraryInformation> GetCandidateLibraries()
        {
            // GetReferencingLibraries returns the transitive closure of referencing assemblies
            // for a given assembly. In our case, we'll gather all assemblies that reference
            // any of the primary Mvc assemblies while ignoring Mvc assemblies.
            return _mvcAssemblyList.SelectMany(_libraryManager.GetReferencingLibraries)
                                   .Distinct()
                                   .Where(IsCandidateLibrary);
        }

        private static Assembly Load(ILibraryInformation library)
        {
            return Assembly.Load(new AssemblyName(library.Name));
        }

        private static bool IsCandidateLibrary(ILibraryInformation library)
        {
            return !_mvcAssemblyList.Contains(library.Name);
        }
    }
}
