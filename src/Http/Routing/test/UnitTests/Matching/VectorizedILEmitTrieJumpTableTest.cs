// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class VectorizedILEmitTrieJumpTableTest : ILEmitTreeJumpTableTestBase
    {
        // We can still run the vectorized implementation on 32 bit, we just
        // don't expect it to be performant - it will still be correct.
        public override bool Vectorize => true;
    }
}
