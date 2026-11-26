// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

public class VectorizedILEmitTrieJumpTableTest : ILEmitTreeJumpTableTestBase
{
    // We can still run the vectorized implementation on 32 bit, we just
    // don't expect it to be performant - it will still be correct.
    public override bool Vectorize => true;

    [Theory]
    [InlineData("admin", 0)]
    [InlineData("ADMIN", 0)]
    [InlineData("prefix/AdMiN/suffix", 7)]
    public void GetDestination_Found_WithBinarySearchAndMixedAsciiAndNonAsciiEntries(string path, int start)
    {
        var table = CreateTable(17, -1, new[]
        {
            ("aaa路x", 13),
            ("bbb路x", 9),
            ("ccc路x", 5),
            ("admin", 1),
        });

        Assert.Equal(1, table.GetDestination(path, new PathSegment(start, 5)));
        Assert.Equal(17, table.GetDestination("admit", new PathSegment(0, 5)));
        Assert.Equal(13, table.GetDestination("aaa路x", new PathSegment(0, 5)));
    }
}
