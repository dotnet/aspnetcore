// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Template
{
    public class TemplateMatcher
    {
        private const string SeparatorString = "/";
        private const char SeparatorChar = '/';

        // Perf: This is a cache to avoid looking things up in 'Defaults' each request.
        private readonly bool[] _hasDefaultValue;
        private readonly object[] _defaultValues;

        private static readonly char[] Delimiters = new char[] { SeparatorChar };
        private RoutePatternMatcher _routePatternMatcher;

        public TemplateMatcher(
            RouteTemplate template,
            RouteValueDictionary defaults)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Template = template;
            Defaults = defaults ?? new RouteValueDictionary();

            // Perf: cache the default value for each parameter (other than complex segments).
            _hasDefaultValue = new bool[Template.Segments.Count];
            _defaultValues = new object[Template.Segments.Count];

            for (var i = 0; i < Template.Segments.Count; i++)
            {
                var segment = Template.Segments[i];
                if (!segment.IsSimple)
                {
                    continue;
                }

                var part = segment.Parts[0];
                if (!part.IsParameter)
                {
                    continue;
                }

                if (Defaults.TryGetValue(part.Name, out var value))
                {
                    _hasDefaultValue[i] = true;
                    _defaultValues[i] = value;
                }
            }

            var routePattern = Template.ToRoutePattern();
            _routePatternMatcher = new RoutePatternMatcher(routePattern, Defaults);
        }

        public RouteValueDictionary Defaults { get; }

        public RouteTemplate Template { get; }

        public bool TryMatch(PathString path, RouteValueDictionary values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return _routePatternMatcher.TryMatch(path, values);
        }
    }
}
