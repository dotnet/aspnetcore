// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Filter.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{

    public class PassThroughConnectionFilter : IConnectionFilter
    {
        public Task OnConnectionAsync(ConnectionFilterContext context)
        {
            context.Connection = new LoggingStream(context.Connection, new TestApplicationErrorLogger());
            return TaskCache.CompletedTask;
        }
    }
}
