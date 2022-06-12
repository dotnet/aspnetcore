// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

internal sealed class StackPolicy : BasePolicy
{
    public StackPolicy(IOptions<QueuePolicyOptions> options)
        : base(options, QueueProcessingOrder.NewestFirst)
    {
    }
}
