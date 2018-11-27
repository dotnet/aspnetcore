// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Enumerates behavior options of the model binding system.
    /// </summary>
    public enum BindingBehavior
    {
        /// <summary>
        /// The property should be model bound if a value is available from the value provider.
        /// </summary>
        Optional = 0,

        /// <summary>
        /// The property should be excluded from model binding.
        /// </summary>
        Never,

        /// <summary>
        /// The property is required for model binding.
        /// </summary>
        Required
    }
}
