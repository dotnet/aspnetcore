// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Serialization;
internal sealed class FormatterEntry
{
    public DateTimeOffset Created { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string?[]> Headers { get; set; } = default!;
    public List<byte[]> Body { get; set; } = default!;
    public string[] Tags { get; set; } = default!;
}
