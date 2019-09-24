// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class SingleEntryAsciiJumpTableTest : SingleEntryJumpTableTestBase
    {
        private protected override JumpTable CreateJumpTable(int defaultDestination, int exitDestination, string text, int destination)
        {
            return new SingleEntryAsciiJumpTable(defaultDestination, exitDestination, text, destination);
        }
    }
}
