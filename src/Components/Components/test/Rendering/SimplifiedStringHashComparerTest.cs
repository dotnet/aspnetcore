// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class SimplifiedStringHashComparerTest
    {
        [Fact]
        public void EqualityIsCaseInsensitive()
        {
            Assert.True(SimplifiedStringHashComparer.Instance.Equals("abc", "ABC"));
        }

        [Fact]
        public void HashCodesAreCaseInsensitive()
        {
            var hash1 = SimplifiedStringHashComparer.Instance.GetHashCode("abc");
            var hash2 = SimplifiedStringHashComparer.Instance.GetHashCode("ABC");
            Assert.Equal(hash1, hash2);
        }
    }
}
