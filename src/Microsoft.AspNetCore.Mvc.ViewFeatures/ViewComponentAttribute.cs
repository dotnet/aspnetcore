// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Indicates the class is a view component and optionally specifies the component's name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ViewComponentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the view component.
        /// </summary>
        public string Name { get; set; }
    }
}
