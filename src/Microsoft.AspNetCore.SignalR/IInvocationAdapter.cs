// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IInvocationAdapter
    {
        Task<InvocationDescriptor> ReadInvocationDescriptorAsync(Stream stream, Func<string, Type[]> getParams);

        Task WriteInvocationResultAsync(InvocationResultDescriptor resultDescriptor, Stream stream);

        Task WriteInvocationDescriptorAsync(InvocationDescriptor invocationDescriptor, Stream stream);
    }
}
