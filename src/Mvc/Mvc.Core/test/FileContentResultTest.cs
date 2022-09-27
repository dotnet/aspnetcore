// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

public class FileContentResultTest : FileContentResultTestBase
{
    protected override Task ExecuteAsync(
        HttpContext httpContext,
        byte[] buffer,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false)
    {
        var result = new FileContentResult(buffer, contentType)
        {
            EntityTag = entityTag,
            LastModified = lastModified,
            EnableRangeProcessing = enableRangeProcessing,
        };

        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<IActionResultExecutor<FileContentResult>, FileContentResultExecutor>()
            .BuildServiceProvider();
        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return result.ExecuteResultAsync(context);
    }
}
