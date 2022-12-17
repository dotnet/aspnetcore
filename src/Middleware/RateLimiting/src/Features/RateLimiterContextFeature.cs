// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RateLimiting.Features;

public class RateLimiterContextFeature : IRateLimiterContextFeature
{
    private readonly RateLimiterContext _context;

    public RateLimiterContextFeature(RateLimiterContext context)
    {
        _context = context;
    }

    public RateLimiterContext Context => _context;
}
