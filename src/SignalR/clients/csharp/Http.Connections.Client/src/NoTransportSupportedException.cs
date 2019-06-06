// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    /// <summary>
    /// Exception thrown during negotiate when there are no supported transports between the client and server.
    /// </summary>
    public class NoTransportSupportedException : Exception
    {
        public NoTransportSupportedException(string message)
            : base(message)
        {
        }
    }
}
