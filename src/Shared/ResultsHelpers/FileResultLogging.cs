// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal
{
    internal interface IFileResultLogger
    {
        void IfUnmodifiedSincePreconditionFailed(
            DateTimeOffset? lastModified,
            DateTimeOffset? ifUnmodifiedSinceDate);

        void IfMatchPreconditionFailed(EntityTagHeaderValue etag);

        void NotEnabledForRangeProcessing();
    }
}
