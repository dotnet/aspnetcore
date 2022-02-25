// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting.Policies;
public class RateLimitingPolicy
{
    private readonly RateLimiter _limiter;
    private readonly QueueProcessingOrder _order;
    private readonly Endpoint _endpoint;
}
