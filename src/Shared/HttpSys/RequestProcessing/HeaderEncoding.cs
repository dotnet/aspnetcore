// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class HeaderEncoding
{
    internal static unsafe string GetString(byte* pBytes, int byteCount, bool useLatin1)
    {
        if (useLatin1)
        {
            return new ReadOnlySpan<byte>(pBytes, byteCount).GetLatin1StringNonNullCharacters();
        }
        else
        {
            return new ReadOnlySpan<byte>(pBytes, byteCount).GetAsciiOrUTF8StringNonNullCharacters(Encoding.UTF8);
        }
    }
}
