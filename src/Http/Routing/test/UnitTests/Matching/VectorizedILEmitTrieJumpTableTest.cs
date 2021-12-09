// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class VectorizedILEmitTrieJumpTableTest : ILEmitTreeJumpTableTestBase
{
    // We can still run the vectorized implementation on 32 bit, we just
    // don't expect it to be performant - it will still be correct.
    public override bool Vectorize => true;
}
