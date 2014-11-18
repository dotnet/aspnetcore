// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

            var candidates =
                types
                .Where(t => IsViewComponentType(t))
                .Select(CreateCandidate);

            // ViewComponent names can either be fully-qualified, or refer to the 'short-name'. If the provided
            // name contains a '.' - then it's a fully-qualified name.
            var matching = new List<ViewComponentCandidate>();
            if (componentName.Contains("."))
            {
                matching.AddRange(candidates.Where(
                    c => string.Equals(c.FullName, componentName, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                matching.AddRange(candidates.Where(
                    c => string.Equals(c.ShortName, componentName, StringComparison.OrdinalIgnoreCase)));
            }

            if (matching.Count == 0)
            {
                return null;
            }
            else if (matching.Count == 1)
            {
                return matching[0].Type;
            }
            else
            {
                var matchedTypes = new List<string>();
                foreach (var candidate in matching)
                {
                    matchedTypes.Add(Resources.FormatViewComponent_AmbiguousTypeMatch_Item(
                        candidate.Type.FullName,
                        candidate.FullName));
                }

                var typeNames = string.Join(Environment.NewLine, matchedTypes);
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AmbiguousTypeMatch(componentName, Environment.NewLine, typeNames));
            }
        }

        protected virtual bool IsViewComponentType([NotNull] TypeInfo typeInfo)
        {
            return ViewComponentConventions.IsComponent(typeInfo);
        }

        private static ViewComponentCandidate CreateCandidate(TypeInfo typeInfo)
        {
            var candidate = new ViewComponentCandidate()
            {
                FullName = ViewComponentConventions.GetComponentFullName(typeInfo),
                ShortName = ViewComponentConventions.GetComponentName(typeInfo),
                Type = typeInfo.AsType(),
            };

            Debug.Assert(!string.IsNullOrEmpty(candidate.FullName));
            var separatorIndex = candidate.FullName.LastIndexOf(".");
            if (separatorIndex >= 0)
            {
                candidate.ShortName = candidate.FullName.Substring(separatorIndex + 1);
            }
            else
            {
                candidate.ShortName = candidate.FullName;
            }

            return candidate;
        }

        private class ViewComponentCandidate
        {
            public string FullName { get; set; }

            public string ShortName { get; set; }

            public Type Type { get; set; }
        }
    }
}
