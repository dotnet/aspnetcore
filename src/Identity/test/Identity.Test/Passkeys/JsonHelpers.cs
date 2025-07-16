// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers.Text;
using System.Text;

namespace Microsoft.AspNetCore.Identity.Test;

internal static class JsonHelpers
{
    public static string ToJsonValue(string? value)
        => value is null ? "null" : $"\"{value}\"";

    public static string ToBase64UrlJsonValue(ReadOnlyMemory<byte>? bytes)
        => !bytes.HasValue ? "null" : $"\"{Base64Url.EncodeToString(bytes.Value.Span)}\"";

    public static string ToBase64UrlJsonValue(string? value)
        => value is null ? "null" : $"\"{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(value))}\"";
}
