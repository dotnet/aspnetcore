// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

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
