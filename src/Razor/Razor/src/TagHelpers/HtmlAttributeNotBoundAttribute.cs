// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Indicates the associated <see cref="ITagHelper"/> property should not be bound to HTML attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class HtmlAttributeNotBoundAttribute : Attribute
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="HtmlAttributeNotBoundAttribute"/> class.
    /// </summary>
    public HtmlAttributeNotBoundAttribute()
    {
    }
}
