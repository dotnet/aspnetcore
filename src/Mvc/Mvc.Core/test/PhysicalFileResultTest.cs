// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc;

public class PhysicalFileResultTest : PhysicalFileResultTestBase
{
    protected override Task ExecuteAsync(
        HttpContext httpContext,
        string path,
        string contentType,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue entityTag = null,
        bool enableRangeProcessing = false)
    {
        var fileResult = new PhysicalFileResult(path, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };

        httpContext.RequestServices = CreateServices();
        var actionContext = new ActionContext(httpContext, new(), new());

        return fileResult.ExecuteResultAsync(actionContext);
    }

    private class TestPhysicalFileResultExecutor : PhysicalFileResultExecutor
    {
        public TestPhysicalFileResultExecutor(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
        }

        protected override FileMetadata GetFileInfo(string path)
        {
            var lastModified = DateTimeOffset.MinValue.AddDays(1);
            return new FileMetadata
            {
                Exists = true,
                Length = 34,
                LastModified = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0))
            };
        }
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<PhysicalFileResult>, TestPhysicalFileResultExecutor>();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services.BuildServiceProvider();
    }
}
