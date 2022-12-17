// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.RateLimiting.Features;

public interface IRateLimiterContextFeature
{
    RateLimiterContext Context { get; }
}

