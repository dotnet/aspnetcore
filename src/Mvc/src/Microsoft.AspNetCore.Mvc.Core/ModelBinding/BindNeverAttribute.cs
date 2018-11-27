// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Indicates that a property should be excluded from model binding. When applied to a property, the model binding
    /// system excludes that property. When applied to a type, the model binding system excludes all properties that
    /// type defines.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class BindNeverAttribute : BindingBehaviorAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="BindNeverAttribute"/> instance.
        /// </summary>
        public BindNeverAttribute()
            : base(BindingBehavior.Never)
        {
        }
    }
}
