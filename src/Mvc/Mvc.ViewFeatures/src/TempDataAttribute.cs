// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Properties decorated with <see cref="TempDataAttribute"/> will have their values stored in
    /// and loaded from the <see cref="ITempDataDictionary"/>. <see cref="TempDataAttribute"/>
    /// is supported on properties of Controllers, Razor Pages, and Razor Page Models.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class TempDataAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the key used to get or add the property from value from <see cref="ITempDataDictionary"/>.
        /// When unspecified, the key is derived from the property name.
        /// </summary>
        public string Key { get; set; }
    }
}
