// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class ControllerActionEndpointDataSourceIdProvider
    {
        private int _nextId = 1;

        internal int CreateId()
        {
            return Interlocked.Increment(ref _nextId);
        }
    }
}
