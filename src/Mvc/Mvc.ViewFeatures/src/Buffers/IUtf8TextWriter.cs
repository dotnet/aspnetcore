// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal interface IUtf8TextWriter
{
    void WriteUtf8(ReadOnlySpan<byte> utf8Value);

    Task WriteUtf8Async(ReadOnlyMemory<byte> utf8Value);
}
