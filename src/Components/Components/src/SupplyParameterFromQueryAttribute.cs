// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Indicates that routing components may supply a value for the parameter from the
    /// current URL querystring. They may also supply further values if the URL querystring changes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SupplyParameterFromQueryAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the querystring parameter. If null, the querystring
        /// parameter is assumed to have the same name as the associated property.
        /// </summary>
        public string? Name { get; set; }
    }
}
