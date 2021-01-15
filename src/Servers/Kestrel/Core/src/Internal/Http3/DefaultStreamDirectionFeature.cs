// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class DefaultStreamDirectionFeature : IStreamDirectionFeature
    {
        public DefaultStreamDirectionFeature(bool canRead, bool canWrite)
        {
            CanRead = canRead;
            CanWrite = canWrite;
        }

        public bool CanRead { get; }

        public bool CanWrite { get; }
    }
}
