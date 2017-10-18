// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePattern
    {
        private const string SeparatorString = "/";

        internal RoutePattern(
            string rawText,
            RoutePatternParameter[] parameters,
            RoutePatternPathSegment[] pathSegments)
        {
            Debug.Assert(parameters != null);
            Debug.Assert(pathSegments != null);

            RawText = rawText;
            Parameters = parameters;
            PathSegments = pathSegments;
        }

        public string RawText { get; }

        public IReadOnlyList<RoutePatternParameter> Parameters { get; }

        public IReadOnlyList<RoutePatternPathSegment> PathSegments { get; }

        public static RoutePattern Parse(string pattern)
        {
            try
            {
                return RoutePatternParser.Parse(pattern);
            }
            catch (RoutePatternException ex)
            {
                throw new ArgumentException(ex.Message, nameof(pattern), ex);
            }
        }

        /// <summary>
        /// Gets the parameter matching the given name.
        /// </summary>
        /// <param name="name">The name of the parameter to match.</param>
        /// <returns>The matching parameter or <c>null</c> if no parameter matches the given name.</returns>
        public RoutePatternParameter GetParameter(string name)
        {
            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return parameter;
                }
            }

            return null;
        }

        private string DebuggerToString()
        {
            return RawText ?? string.Join(SeparatorString, PathSegments.Select(s => s.DebuggerToString()));
        }
    }
}
