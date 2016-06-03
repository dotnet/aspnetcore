// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class NoOpConnectionFilter : IConnectionFilter
    {
        public Task OnConnectionAsync(ConnectionFilterContext context)
        {
            return TaskUtilities.CompletedTask;
        }
    }
}
