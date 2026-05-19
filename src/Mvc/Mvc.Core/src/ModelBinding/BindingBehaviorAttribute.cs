// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
