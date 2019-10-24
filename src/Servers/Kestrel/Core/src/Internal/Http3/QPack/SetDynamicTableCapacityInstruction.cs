// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    public struct SetDynamicTableCapacityInstruction
    {
        public SetDynamicTableCapacityInstruction(Span<byte> integerToDecode)
        {
            // Set max table size based on integer tod decode.
        }
    }
}
