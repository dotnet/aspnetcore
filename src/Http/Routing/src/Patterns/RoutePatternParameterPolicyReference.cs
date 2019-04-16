// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// The parsed representation of a policy in a <see cref="RoutePattern"/> parameter. Instances
    /// of <see cref="RoutePatternParameterPolicyReference"/> are immutable.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternParameterPolicyReference
    {
        internal RoutePatternParameterPolicyReference(string content)
        {
            Content = content;
        }

        internal RoutePatternParameterPolicyReference(IParameterPolicy parameterPolicy)
        {
            ParameterPolicy = parameterPolicy;
        }

        /// <summary>
        /// Gets the constraint text.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets a pre-existing <see cref="IParameterPolicy"/> that was used to construct this reference.
        /// </summary>
        public IParameterPolicy ParameterPolicy { get; }

        private string DebuggerToString()
        {
            return Content;
        }
    }
}