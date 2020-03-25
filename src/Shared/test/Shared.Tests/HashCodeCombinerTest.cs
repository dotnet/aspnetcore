// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class HashCodeCombinerTest
    {
        [Fact]
        public void GivenTheSameInputs_ItProducesTheSameOutput()
        {
            var hashCode1 = new HashCodeCombiner();
            var hashCode2 = new HashCodeCombiner();

            hashCode1.Add(42);
            hashCode1.Add("foo");
            hashCode2.Add(42);
            hashCode2.Add("foo");

            Assert.Equal(hashCode1.CombinedHash, hashCode2.CombinedHash);
        }

        [Fact]
        public void HashCode_Is_OrderSensitive()
        {
            var hashCode1 = HashCodeCombiner.Start();
            var hashCode2 = HashCodeCombiner.Start();

            hashCode1.Add(42);
            hashCode1.Add("foo");

            hashCode2.Add("foo");
            hashCode2.Add(42);

            Assert.NotEqual(hashCode1.CombinedHash, hashCode2.CombinedHash);
        }
    }
}
