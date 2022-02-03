// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.DataProtection;

internal static class EncodingUtil
{
    // UTF8 encoding that fails on invalid chars
    public static readonly UTF8Encoding SecureUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
