// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result
{
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
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .BuildServiceProvider();

            return result.ExecuteAsync(httpContext);
        }
    }
}
