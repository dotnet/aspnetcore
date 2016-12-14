// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IInvocationAdapter
    {
        Task<InvocationMessage> ReadMessageAsync(Stream stream, IInvocationBinder binder, CancellationToken cancellationToken);

        Task WriteMessageAsync(InvocationMessage message, Stream stream, CancellationToken cancellationToken);
    }
}
