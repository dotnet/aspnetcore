// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class DefaultStreamDirectionFeature : IStreamDirectionFeature
{
    public DefaultStreamDirectionFeature(bool canRead, bool canWrite)
    {
        CanRead = canRead;
        CanWrite = canWrite;
    }

    public bool CanRead { get; }

    public bool CanWrite { get; }
}
