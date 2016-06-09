// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Indicates that the property is required for model binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class BindRequiredAttribute : BindingBehaviorAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="BindRequiredAttribute"/> instance.
        /// </summary>
        public BindRequiredAttribute()
            : base(BindingBehavior.Required)
        {
        }
    }
}
