// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR.Specification.Tests;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultHubLifetimeManagerTests : HubLifetimeManagerTestsBase<MyHub>
    {
        public override HubLifetimeManager<MyHub> CreateNewHubLifetimeManager()
        {
            return new DefaultHubLifetimeManager<MyHub>(new Logger<DefaultHubLifetimeManager<MyHub>>(NullLoggerFactory.Instance));
        }
    }
}
