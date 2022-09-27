// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.Cng;

internal unsafe interface IBCryptGenRandom
{
    void GenRandom(byte* pbBuffer, uint cbBuffer);
}
