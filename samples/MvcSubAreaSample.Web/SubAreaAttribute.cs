// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace MvcSubAreaSample.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SubAreaAttribute : RouteConstraintAttribute
    {
        public SubAreaAttribute(string name)
            : base("subarea", name, blockNonAttributedActions: true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("SubArea name must not be null or empty.", nameof(name));
            }
        }
    }
}
