// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
