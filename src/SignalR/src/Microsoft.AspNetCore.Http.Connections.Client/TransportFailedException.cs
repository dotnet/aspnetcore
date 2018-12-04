// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    /// <summary>
    /// Exception thrown during negotiate when a transport fails to connect.
    /// </summary>
    public class TransportFailedException : Exception
    {
        public string TransportType { get; }

        public TransportFailedException(string transportType, string message, Exception innerException = null)
            : base($"{transportType} failed: {message}", innerException)
        {
            TransportType = transportType;
        }
    }
}
