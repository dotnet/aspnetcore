// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, MemoryPoolBlock block)
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(block.Array, block.Data.Offset, block.Data.Count)) != 0)
            {
                await destination.WriteAsync(block.Array, block.Data.Offset, bytesRead);
            }
        }
    }
}
