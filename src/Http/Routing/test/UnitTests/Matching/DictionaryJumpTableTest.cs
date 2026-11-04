// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

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
