// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IFrameControl
    {
        void ProduceContinue();
        void Write(ArraySegment<byte> data);
        Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken);
        void Flush();
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
