// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.DataProtection.SP800_108;

internal unsafe interface ISP800_108_CTR_HMACSHA512Provider : IDisposable
{
    void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey);
}
