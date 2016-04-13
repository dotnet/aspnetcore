// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An filter that skips antiforgery token validation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IgnoreAntiforgeryTokenAttribute : Attribute, IAntiforgeryPolicy, IOrderedFilter
    {
        /// <inheritdoc />
        public int Order { get; set; }
    }
}
