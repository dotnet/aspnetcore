// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    internal class Http3Limits
    {
        private int _headerTableSize = 0;
        private int _blockedStreams = 0;

        /// <summary>
        /// Limits the size of the header compression table, in octets, the QPACK decoder on the server can use.
        /// <para>
        /// Defaults to 0.
        /// </para>
        /// </summary>
        internal int HeaderTableSize
        {
            get => _headerTableSize;
        }

        /// <summary>
        /// Limits the number of blocked streams for a connection that are waiting for a dynamic table update.
        /// <para>
        /// Defaults to 0.
        /// </para>
        /// </summary>
        internal int BlockedStreams
        {
            get => _blockedStreams;
        }
    }
}
