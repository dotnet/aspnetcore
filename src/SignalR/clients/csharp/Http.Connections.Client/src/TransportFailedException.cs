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
        /// <summary>
        /// The name of the transport that failed to connect.
        /// </summary>
        public string TransportType { get; }

        /// <summary>
        /// Constructs a <see cref="TransportFailedException"/>.
        /// </summary>
        /// <param name="transportType">The name of the transport that failed to connect.</param>
        /// <param name="message">The reason the transport failed.</param>
        /// <param name="innerException">An optional extra exception if one was thrown while trying to connect.</param>
        public TransportFailedException(string transportType, string message, Exception? innerException = null)
            : base($"{transportType} failed: {message}", innerException)
        {
            TransportType = transportType;
        }
    }
}
