// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
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
}