// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Limits only applicable to HTTP/2 connections.
    /// </summary>
    public class Http2Limits
    {
        private int _maxStreamsPerConnection = 100;

        /// <summary>
        /// Limits the number of concurrent request streams per HTTP/2 connection. Excess streams will be refused.
        /// <para>
        /// Defaults to 100
        /// </para>
        /// </summary>
        public int MaxStreamsPerConnection
        {
            get => _maxStreamsPerConnection;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
                }
                _maxStreamsPerConnection = value;
            }
        }
    }
}
