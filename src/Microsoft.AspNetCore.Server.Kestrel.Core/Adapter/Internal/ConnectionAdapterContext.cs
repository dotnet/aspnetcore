// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    // Even though this only includes the non-adapted ConnectionStream currently, this is a context in case
    // we want to add more connection metadata later.
    public class ConnectionAdapterContext
    {
        internal ConnectionAdapterContext(IFeatureCollection features, Stream connectionStream)
        {
            Features = features;
            ConnectionStream = connectionStream;
        }

        public IFeatureCollection Features { get; }

        public Stream ConnectionStream { get; }
    }
}
