// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    public struct StreamCancellationInstruction
    {
        public StreamCancellationInstruction(Span<byte> buffer)
        {
            // 01, then 6 bit prefix integer

        }
    }
}
