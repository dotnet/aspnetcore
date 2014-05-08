// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
