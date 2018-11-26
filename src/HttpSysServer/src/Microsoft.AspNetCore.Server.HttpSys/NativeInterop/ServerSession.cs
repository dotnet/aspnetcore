// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class ServerSession : IDisposable
    {
        internal unsafe ServerSession()
        {
            ulong serverSessionId = 0;
            var statusCode = HttpApi.HttpCreateServerSession(
                HttpApi.Version, &serverSessionId, 0);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                throw new HttpSysException((int)statusCode);
            }

            Debug.Assert(serverSessionId != 0, "Invalid id returned by HttpCreateServerSession");

            Id = new HttpServerSessionHandle(serverSessionId);
        }

        public HttpServerSessionHandle Id { get; private set; }

        public void Dispose()
        {
            Id.Dispose();
        }
    }
}
