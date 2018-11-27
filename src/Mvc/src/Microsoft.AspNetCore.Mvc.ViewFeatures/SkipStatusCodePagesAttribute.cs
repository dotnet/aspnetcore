// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A filter that prevents execution of the StatusCodePages middleware.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SkipStatusCodePagesAttribute : Attribute, IResourceFilter
    {
        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var statusCodeFeature = context.HttpContext.Features.Get<IStatusCodePagesFeature>();
            if (statusCodeFeature != null)
            {
                // Turn off the StatusCodePages feature.
                statusCodeFeature.Enabled = false;
            }
        }
    }
}