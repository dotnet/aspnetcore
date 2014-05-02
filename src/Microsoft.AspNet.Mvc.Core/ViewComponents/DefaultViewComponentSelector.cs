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
using System.Linq;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentSelector : IViewComponentSelector
    {
        private readonly IControllerAssemblyProvider _assemblyProvider;

        public DefaultViewComponentSelector(IControllerAssemblyProvider assemblyProvider)
        {
            _assemblyProvider = assemblyProvider;
        }

        public Type SelectComponent([NotNull] string componentName)
        {
            var assemblies = _assemblyProvider.CandidateAssemblies;
            var types = assemblies.SelectMany(a => a.DefinedTypes);

            var components = 
                types
                .Where(ViewComponentConventions.IsComponent)
                .Select(c => new {Name = ViewComponentConventions.GetComponentName(c), Type = c.AsType()});

            var matching = 
                components
                .Where(c => string.Equals(c.Name, componentName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matching.Length == 0)
            {
                return null;
            }
            else if (matching.Length == 1)
            {
                return matching[0].Type;
            }
            else
            {
                var typeNames = string.Join(Environment.NewLine, matching.Select(t => t.Type.FullName));
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AmbiguousTypeMatch(componentName, typeNames));
            }
        }
    }
}
