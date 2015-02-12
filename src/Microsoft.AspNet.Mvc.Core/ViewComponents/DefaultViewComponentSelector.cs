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

        private ViewComponentCandidateCache _cache;

        public DefaultViewComponentSelector(IAssemblyProvider assemblyProvider)
        {
            _assemblyProvider = assemblyProvider;
        }

        public Type SelectComponent([NotNull] string componentName)
        {
            if (_cache == null)
            {
                var assemblies = _assemblyProvider.CandidateAssemblies;
                var types = assemblies.SelectMany(a => a.DefinedTypes);

                var candidates =
                    types
                    .Where(IsViewComponentType)
                    .Select(CreateCandidate)
                    .ToArray();

                _cache = new ViewComponentCandidateCache(candidates);
            }

            // ViewComponent names can either be fully-qualified, or refer to the 'short-name'. If the provided
            // name contains a '.' - then it's a fully-qualified name.
            var matching = new List<ViewComponentCandidate>();
            if (componentName.Contains("."))
            {
                return _cache.SelectByFullName(componentName);
            }
            else
            {
                return _cache.SelectByShortName(componentName);
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
            var separatorIndex = candidate.FullName.LastIndexOf('.');
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

        private class ViewComponentCandidateCache
        {
            private readonly ILookup<string, ViewComponentCandidate> _lookupByShortName;
            private readonly ILookup<string, ViewComponentCandidate> _lookupByFullName;

            public ViewComponentCandidateCache(ViewComponentCandidate[] candidates)
            {
                _lookupByShortName = candidates.ToLookup(c => c.ShortName, c => c);
                _lookupByFullName = candidates.ToLookup(c => c.FullName, c => c);
            }

            public Type SelectByShortName(string name)
            {
                return Select(_lookupByShortName, name);
            }

            public Type SelectByFullName(string name)
            {
                return Select(_lookupByFullName, name);
            }

            private static Type Select(ILookup<string, ViewComponentCandidate> candidates, string name)
            {
                var matches = candidates[name];

                var count = matches.Count();
                if (count == 0)
                {
                    return null;
                }
                else if (count == 1)
                {
                    return matches.Single().Type;
                }
                else
                {
                    var matchedTypes = new List<string>();
                    foreach (var candidate in matches)
                    {
                        matchedTypes.Add(Resources.FormatViewComponent_AmbiguousTypeMatch_Item(
                            candidate.Type.FullName,
                            candidate.FullName));
                    }

                    var typeNames = string.Join(Environment.NewLine, matchedTypes);
                    throw new InvalidOperationException(
                        Resources.FormatViewComponent_AmbiguousTypeMatch(name, Environment.NewLine, typeNames));
                }
            }
        }
    }
}
