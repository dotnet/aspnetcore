// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Determines the behavior for processing empty bodies during input formatting.
    /// </summary>
    public enum EmptyBodyBehavior
    {
        /// <summary>
        /// Uses the framework default behavior for processing empty bodies.
        /// This is typically configured using <c>MvcOptions.AllowEmptyInputInBodyModelBinding</c>.
        /// </summary>
        Default,
        
        /// <summary>
        /// Empty bodies are treated as valid inputs.
        /// </summary>
        Allow,

        /// <summary>
        /// Empty bodies are treated as invalid inputs.
        /// </summary>
        Disallow,
    }
}
