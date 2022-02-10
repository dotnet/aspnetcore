// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Result;

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
            GetFileInfoWrapper = (path) =>
            {
                var lastModified = DateTimeOffset.MinValue.AddDays(1);
                return new()
                {
                    Exists = true,
                    Length = 34,
                    LastWriteTimeUtc = new DateTimeOffset(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second, TimeSpan.FromSeconds(0))
                };
            }
        };

        return fileResult.ExecuteAsync(httpContext);
    }
}
