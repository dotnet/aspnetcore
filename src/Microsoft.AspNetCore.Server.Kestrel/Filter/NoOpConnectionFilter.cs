// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class NoOpConnectionFilter : IConnectionFilter
    {
        public Task OnConnectionAsync(ConnectionFilterContext context)
        {
            return TaskCache.CompletedTask;
        }
    }
}
