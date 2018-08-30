// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if IL_EMIT
namespace Microsoft.AspNetCore.Routing.Matching
{
    public class NonVectorizedILEmitTrieJumpTableTest : ILEmitTreeJumpTableTestBase
    {
        public override bool Vectorize => false;
    }
}
#endif
