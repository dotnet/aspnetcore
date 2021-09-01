// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
