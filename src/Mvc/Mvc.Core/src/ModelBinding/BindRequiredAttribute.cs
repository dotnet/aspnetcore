// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Indicates that a property is required for model binding. When applied to a property, the model binding system
    /// requires a value for that property. When applied to a type, the model binding system requires values for all
    /// properties that type defines.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
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
