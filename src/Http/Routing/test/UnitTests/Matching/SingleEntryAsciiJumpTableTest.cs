// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class SingleEntryAsciiJumpTableTest : SingleEntryJumpTableTestBase
{
    private protected override JumpTable CreateJumpTable(int defaultDestination, int exitDestination, string text, int destination)
    {
        return new SingleEntryAsciiJumpTable(defaultDestination, exitDestination, text, destination);
    }
}
