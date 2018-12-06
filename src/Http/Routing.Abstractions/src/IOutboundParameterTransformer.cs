// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines the contract that a class must implement to transform route values while building
    /// a URI.
    /// </summary>
    public interface IOutboundParameterTransformer : IParameterPolicy
    {
        /// <summary>
        /// Transforms the specified route value to a string for inclusion in a URI.
        /// </summary>
        /// <param name="value">The route value to transform.</param>
        /// <returns>The transformed value.</returns>
        string TransformOutbound(object value);
    }
}
