// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal;

internal readonly struct FileResultInfo
{
    public string ContentType { get; init; }

    public string FileDownloadName { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public EntityTagHeaderValue? EntityTag { get; init; }

    public bool EnableRangeProcessing { get; init; }
}
