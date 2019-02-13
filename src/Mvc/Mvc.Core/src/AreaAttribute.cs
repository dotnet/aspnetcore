// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies the area containing a controller or action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AreaAttribute : RouteValueAttribute
    {
        /// <summary>
        /// Initializes a new <see cref="AreaAttribute"/> instance.
        /// </summary>
        /// <param name="areaName">The area containing the controller or action.</param>
        public AreaAttribute(string areaName)
            : base("area", areaName)
        {
            if (string.IsNullOrEmpty(areaName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(areaName));
            }
        }
    }
}
