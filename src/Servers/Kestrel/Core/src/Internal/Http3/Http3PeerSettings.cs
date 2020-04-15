// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3PeerSettings
    {
        internal const uint DefaultMaxFrameSize = 16 * 1024;

        public static int MinAllowedMaxFrameSize { get; internal set; } = 16 * 1024;
    }
}
