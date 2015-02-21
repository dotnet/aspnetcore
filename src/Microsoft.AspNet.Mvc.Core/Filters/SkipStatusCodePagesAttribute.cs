// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Filter to prevent StatusCodePages middleware to handle responses.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SkipStatusCodePagesAttribute : Attribute, IResourceFilter
    {
        /// <inheritdoc />
        public void OnResourceExecuted([NotNull]ResourceExecutedContext context)
        {
        }

        /// <inheritdoc />
        public void OnResourceExecuting([NotNull]ResourceExecutingContext context)
        {
            var statusCodeFeature = context.HttpContext.GetFeature<IStatusCodePagesFeature>();
            if (statusCodeFeature != null)
            {
                // Turn off the StatusCodePages feature.
                statusCodeFeature.Enabled = false;
            }
        }
    }
}