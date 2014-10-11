// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentSelector : IViewComponentSelector
    {
        private readonly IAssemblyProvider _assemblyProvider;

        public DefaultViewComponentSelector(IAssemblyProvider assemblyProvider)
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
                .Select(c => new { Name = ViewComponentConventions.GetComponentName(c), Type = c.AsType() });

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
