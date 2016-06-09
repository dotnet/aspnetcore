// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Indicates that the property should be excluded from model binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
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
