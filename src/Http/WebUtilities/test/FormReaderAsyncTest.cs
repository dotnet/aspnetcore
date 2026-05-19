// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

public class FormReaderAsyncTest : FormReaderTests
{
    protected override async Task<Dictionary<string, StringValues>> ReadFormAsync(FormReader reader)
    {
        return await reader.ReadFormAsync();
    }

    protected override async Task<KeyValuePair<string, string>?> ReadPair(FormReader reader)
    {
        return await reader.ReadNextPairAsync();
    }
}
