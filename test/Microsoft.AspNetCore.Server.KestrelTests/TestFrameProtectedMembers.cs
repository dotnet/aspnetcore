// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class TestFrameProtectedMembers<TContext> : Frame<TContext>
    {
        public TestFrameProtectedMembers(IHttpApplication<TContext> application, ConnectionContext context)
            : base(application, context)
        {
        }

        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }
    }
}
