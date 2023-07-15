// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SocketTransportOptions
    {
        private const string FinOnErrorSwitch = "Microsoft.AspNetCore.Server.Kestrel.FinOnError";
        private static readonly bool _finOnError;

        static SocketTransportOptions()
        {
            AppContext.TryGetSwitch(FinOnErrorSwitch, out _finOnError);
        }

        // Opt-out flag for back compat. Remove in 9.0 (or make public).
        internal bool FinOnError { get; set; } = _finOnError;

        /// <summary>
        /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);
    }
}
