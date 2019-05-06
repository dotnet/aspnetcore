// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class DictionaryJumpTableTest : MultipleEntryJumpTableTest
    {
        internal override JumpTable CreateTable(
            int defaultDestination,
            int exitDestination,
            params (string text, int destination)[] entries)
        {
            return new DictionaryJumpTable(defaultDestination, exitDestination, entries);
        }
    }
}
