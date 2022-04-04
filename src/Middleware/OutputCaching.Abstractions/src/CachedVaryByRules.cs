// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

public class CachedVaryByRules
{
    public Dictionary<string, string> VaryByCustom { get; } = new(StringComparer.OrdinalIgnoreCase);

    public StringValues Headers { get; set; }

    public StringValues QueryKeys { get; set; }

    // Normalized version of VaryByCustom
    public StringValues VaryByPrefix { get; set; }
}
