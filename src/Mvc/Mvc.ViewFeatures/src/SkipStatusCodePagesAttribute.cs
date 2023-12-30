// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that prevents execution of the StatusCodePages middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SkipStatusCodePagesAttribute : Attribute, IResourceFilter, ISkipStatusCodePagesMetadata
{
    /// <inheritdoc />
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    /// <inheritdoc />
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var statusCodeFeature = context.HttpContext.Features.Get<IStatusCodePagesFeature>();
        if (statusCodeFeature != null)
        {
            // Turn off the StatusCodePages feature.
            statusCodeFeature.Enabled = false;
        }
    }
}
