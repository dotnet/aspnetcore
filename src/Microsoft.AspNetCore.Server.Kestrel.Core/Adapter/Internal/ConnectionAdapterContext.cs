// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    // Even though this only includes the non-adapted ConnectionStream currently, this is a context in case
    // we want to add more connection metadata later.
    public class ConnectionAdapterContext
    {
        internal ConnectionAdapterContext(Stream connectionStream)
        {
            ConnectionStream = connectionStream;
        }

        public Stream ConnectionStream { get; }
    }
}
