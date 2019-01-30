// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Represents a parsed route template with default values and constraints.
    /// Use <see cref="RoutePatternFactory"/> to create <see cref="RoutePattern"/>
    /// instances. Instances of <see cref="RoutePattern"/> are immutable.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePattern
    {
        private const string SeparatorString = "/";

        internal RoutePattern(
            string rawText,
            IReadOnlyDictionary<string, object> defaults,
            IReadOnlyDictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>> parameterPolicies,
            IReadOnlyDictionary<string, object> requiredValues,
            IReadOnlyList<RoutePatternParameterPart> parameters,
            IReadOnlyList<RoutePatternPathSegment> pathSegments)
        {
            Debug.Assert(defaults != null);
            Debug.Assert(parameterPolicies != null);
            Debug.Assert(parameters != null);
            Debug.Assert(requiredValues != null);
            Debug.Assert(pathSegments != null);

            RawText = rawText;
            Defaults = defaults;
            ParameterPolicies = parameterPolicies;
            RequiredValues = requiredValues;
            Parameters = parameters;
            PathSegments = pathSegments;

            InboundPrecedence = RoutePrecedence.ComputeInbound(this);
            OutboundPrecedence = RoutePrecedence.ComputeOutbound(this);
        }

        /// <summary>
        /// Gets the set of default values for the route pattern.
        /// The keys of <see cref="Defaults"/> are the route parameter names.
        /// </summary>
        public IReadOnlyDictionary<string, object> Defaults { get; }

        /// <summary>
        /// Gets the set of parameter policy references for the route pattern.
        /// The keys of <see cref="ParameterPolicies"/> are the route parameter names.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>> ParameterPolicies { get; }

        /// <summary>
        /// Gets a collection of route values that must be provided for this route pattern to be considered
        /// applicable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="RequiredValues"/> allows a framework to substitute route values into a parameterized template
        /// so that the same route template specification can be used to create multiple route patterns.
        /// <example>
        /// This example shows how a route template can be used with required values to substitute known
        /// route values for parameters.
        /// <code>
        /// Route Template: "{controller=Home}/{action=Index}/{id?}"
        /// Route Values: { controller = "Store", action = "Index" }
        /// </code>
        /// 
        /// A route pattern produced in this way will match and generate URL paths like: <c>/Store</c>, 
        /// <c>/Store/Index</c>, and <c>/Store/Index/17</c>.
        /// </example>
        /// </para>
        /// </remarks>
        public IReadOnlyDictionary<string, object> RequiredValues { get; }

        /// <summary>
        /// Gets the precedence value of the route pattern for URL matching.
        /// </summary>
        /// <remarks>
        /// Precedence is a computed value based on the structure of the route pattern
        /// used for building URL matching data structures.
        /// </remarks>
        public decimal InboundPrecedence { get; }

        /// <summary>
        /// Gets the precedence value of the route pattern for URL generation.
        /// </summary>
        /// <remarks>
        /// Precedence is a computed value based on the structure of the route pattern
        /// used for building URL generation data structures.
        /// </remarks>
        public decimal OutboundPrecedence { get; }

        /// <summary>
        /// Gets the raw text supplied when parsing the route pattern. May be null.
        /// </summary>
        public string RawText { get; }

        /// <summary>
        /// Gets the list of route parameters.
        /// </summary>
        public IReadOnlyList<RoutePatternParameterPart> Parameters { get; }

        /// <summary>
        /// Gets the list of path segments.
        /// </summary>
        public IReadOnlyList<RoutePatternPathSegment> PathSegments { get; }

        /// <summary>
        /// Gets the parameter matching the given name.
        /// </summary>
        /// <param name="name">The name of the parameter to match.</param>
        /// <returns>The matching parameter or <c>null</c> if no parameter matches the given name.</returns>
        public RoutePatternParameterPart GetParameter(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

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

        internal string DebuggerToString()
        {
            return RawText ?? string.Join(SeparatorString, PathSegments.Select(s => s.DebuggerToString()));
        }
    }
}
