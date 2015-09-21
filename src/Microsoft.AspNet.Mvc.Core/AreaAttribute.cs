// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AreaAttribute : RouteConstraintAttribute
    {
        public AreaAttribute(string areaName)
            : base("area", areaName, blockNonAttributedActions: true)
        {
            if (string.IsNullOrEmpty(areaName))
            {
                throw new ArgumentException("Area name must not be empty", nameof(areaName));
            }
        }
    }
}
