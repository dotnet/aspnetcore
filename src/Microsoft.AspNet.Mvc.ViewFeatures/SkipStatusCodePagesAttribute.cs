// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Filter to prevent StatusCodePages middleware to handle responses.
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