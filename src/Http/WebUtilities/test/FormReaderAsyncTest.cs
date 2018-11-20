// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
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
}