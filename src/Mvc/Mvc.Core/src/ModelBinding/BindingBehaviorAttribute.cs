// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Specifies the <see cref="BindingBehavior"/> that should be applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class BindingBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new <see cref="BindingBehaviorAttribute"/> instance.
        /// </summary>
        /// <param name="behavior">The <see cref="BindingBehavior"/> to apply.</param>
        public BindingBehaviorAttribute(BindingBehavior behavior)
        {
            Behavior = behavior;
        }

        /// <summary>
        /// Gets the <see cref="BindingBehavior"/> to apply.
        /// </summary>
        public BindingBehavior Behavior { get; }
    }
}
