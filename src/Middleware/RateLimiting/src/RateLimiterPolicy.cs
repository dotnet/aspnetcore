// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class RateLimiterPolicy : IRateLimiterPolicy<AspNetKey>
{
    [Required]
    public Func<HttpContext, RateLimitPartition<AspNetKey>> Partitioner { get; init; }

    [Required]
    public string PolicyName { get; init; }

    public PartitionKeyScope KeyScope { get; init; } = PartitionKeyScope.Policy;

    public int CustomRejectionStatusCode => throw new NotImplementedException();

    public RateLimitPartition<AspNetKey> GetPartition(HttpContext httpContext)
    {
        throw new NotImplementedException();
    }
}
internal enum PartitionKeyScope
{
    Policy,
    Global
}
