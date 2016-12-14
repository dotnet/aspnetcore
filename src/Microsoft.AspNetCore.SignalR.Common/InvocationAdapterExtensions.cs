// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public static class InvocationAdapterExtensions
    {
        public static Task<InvocationMessage> ReadMessageAsync(this IInvocationAdapter self, Stream stream, IInvocationBinder binder) => self.ReadMessageAsync(stream, binder, CancellationToken.None);

        public static Task WriteMessageAsync(this IInvocationAdapter self, InvocationMessage message, Stream stream) => self.WriteMessageAsync(message, stream, CancellationToken.None);
    }
}
