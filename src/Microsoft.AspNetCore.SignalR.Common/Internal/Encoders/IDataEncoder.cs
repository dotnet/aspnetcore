// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public interface IDataEncoder
    {
        byte[] Encode(byte[] payload);
        bool TryDecode(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> data);
    }
}
