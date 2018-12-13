// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal interface IAsyncIOEngine: IDisposable
    {
        ValueTask<int> ReadAsync(Memory<byte> memory);
        ValueTask<int> WriteAsync(ReadOnlySequence<byte> data);
        ValueTask FlushAsync(bool moreData);
        void NotifyCompletion(int hr, int bytes);
    }
}
