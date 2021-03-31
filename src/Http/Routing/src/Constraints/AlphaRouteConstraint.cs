// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constrains a route parameter to contain only lowercase or uppercase letters A through Z in the English alphabet.
    /// </summary>
    public class AlphaRouteConstraint : RegexRouteConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaRouteConstraint" /> class.
        /// </summary>
        public AlphaRouteConstraint() : base(@"^[a-z]*$")
        {
        }
    }
}