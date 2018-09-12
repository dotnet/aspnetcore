// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines the contract that a class must implement to transform parameter values.
    /// </summary>
    public interface IParameterTransformer : IParameterPolicy
    {
        /// <summary>
        /// Transforms the specified parameter value.
        /// </summary>
        /// <param name="value">The parameter value to transform.</param>
        /// <returns>The transformed value.</returns>
        string Transform(string value);
    }
}
