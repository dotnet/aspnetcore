// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Filter to prevent StatusCodePages middleware to handle responses.
    /// </summary>
    public class SkipStatusCodePagesAttribute : ResultFilterAttribute
    {
        /// <inheritdoc />
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            var statusCodeFeature = context.HttpContext.GetFeature<IStatusCodePagesFeature>();
            if (statusCodeFeature != null)
            {
                // Turn off the StatusCodePages feature.
                statusCodeFeature.Enabled = false;
            }

            base.OnResultExecuted(context);
        }
    }
}