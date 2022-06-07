// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class RateLimiterPolicy
{
    [Required]
    public Func<HttpContext, RateLimitPartition<AspNetKey>> Partitioner { get; init; }

    [Required]
    public string PolicyName { get; init; }

    public PartitionKeyScope KeyScope { get; init; } = PartitionKeyScope.Policy;
}
internal enum PartitionKeyScope
{
    Policy,
    Global
}
