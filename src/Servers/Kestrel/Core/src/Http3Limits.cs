// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class Http3Limits
    {
        private long _headerTableSize = 0;
        private long _blockedStreams = 0;

        /// <summary>
        /// Limits the size of the header compression table, in octets, the QPACK decoder on the server can use.
        /// <para>
        /// Defaults to 0.
        /// </para>
        /// </summary>
        public long HeaderTableSize
        {
            get => _headerTableSize;
            set => throw new NotImplementedException("Dynamic table is not supported in HTTP/3.");
        }

        /// <summary>
        /// Limits the number of blocked streams for a connection that are waiting for a dynamic table update.
        /// <para>
        /// Defaults to 0.
        /// </para>
        /// </summary>
        public long BlockedStreams
        {
            get => _blockedStreams;
            set => throw new NotImplementedException("Dynamic table is not supported in HTTP/3.");
        }
    }
}
