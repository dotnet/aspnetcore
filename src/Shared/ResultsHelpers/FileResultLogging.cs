// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal;

internal interface IFileResultLogger
{
    void IfUnmodifiedSincePreconditionFailed(
        DateTimeOffset? lastModified,
        DateTimeOffset? ifUnmodifiedSinceDate);

    void IfMatchPreconditionFailed(EntityTagHeaderValue etag);

    void NotEnabledForRangeProcessing();
}
