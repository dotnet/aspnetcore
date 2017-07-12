// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public interface IConnectionContext
    {
        string ConnectionId { get; }
        IPipeWriter Input { get; }
        IPipeReader Output { get; }

        // TODO: Remove these (https://github.com/aspnet/KestrelHttpServer/issues/1772)
        void OnConnectionClosed(Exception ex);
        void Abort(Exception ex);
    }
}
