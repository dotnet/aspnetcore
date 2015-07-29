// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class ShutdownNotImplemented : IApplicationShutdown
    {
        public CancellationToken ShutdownRequested
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void RequestShutdown()
        {
            throw new NotImplementedException();
        }
    }
}
