// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComponentsAIClaimApp.Data;

internal static class ClaimLimits
{
    internal const int MaximumPhotoCount = 6;
    internal const long MaximumPhotoBytes = 8 * 1024 * 1024;
    internal const long MaximumEvidenceBytes = 24 * 1024 * 1024;
    internal const long MaximumSerializedRequestBytes = 40 * 1024 * 1024;
}
