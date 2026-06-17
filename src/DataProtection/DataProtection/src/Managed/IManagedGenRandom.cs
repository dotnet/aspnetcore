// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.DataProtection.Managed;

internal interface IManagedGenRandom
{
    byte[] GenRandom(int numBytes);

#if NET10_0_OR_GREATER
    void GenRandom(Span<byte> target);
#endif
}
