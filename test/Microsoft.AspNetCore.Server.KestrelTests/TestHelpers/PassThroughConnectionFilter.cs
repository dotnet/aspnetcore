// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Filter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.KestrelTests
{

    public class PassThroughConnectionFilter : IConnectionFilter
    {
        public Task OnConnectionAsync(ConnectionFilterContext context)
        {
            context.Connection = new LoggingStream(context.Connection, new TestApplicationErrorLogger());
            return TaskUtilities.CompletedTask;
        }
    }
}
