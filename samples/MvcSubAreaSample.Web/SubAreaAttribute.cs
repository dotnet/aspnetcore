// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace MvcSubAreaSample.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SubAreaAttribute : RouteValueAttribute
    {
        public SubAreaAttribute(string name)
            : base("subarea", name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("SubArea name must not be null or empty.", nameof(name));
            }
        }
    }
}
