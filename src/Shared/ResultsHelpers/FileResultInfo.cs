// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal
{
    internal readonly struct FileResultInfo
    {
        public string ContentType { get; init; }

        public string FileDownloadName { get; init; }

        public DateTimeOffset? LastModified { get; init; }

        public EntityTagHeaderValue? EntityTag { get; init; }

        public bool EnableRangeProcessing { get; init; }
    }
}
