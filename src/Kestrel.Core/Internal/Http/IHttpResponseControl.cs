// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpResponseControl
    {
        void ProduceContinue();
        Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
