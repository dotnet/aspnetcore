// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ReferenceEqualityComparerTest
    {
        [Fact]
        public void Equals_ReturnsTrue_ForSameObject()
        {
            var o = new object();
            Assert.True(ReferenceEqualityComparer.Instance.Equals(o, o));
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentObject()
        {
            var o1 = new object();
            var o2 = new object();

            Assert.False(ReferenceEqualityComparer.Instance.Equals(o1, o2));
        }

        [Fact]
        public void Equals_DoesntCall_OverriddenEqualsOnTheType()
        {
            var t1 = new TypeThatOverridesEquals();
            var t2 = new TypeThatOverridesEquals();

            // Act & Assert (does not throw)
            ReferenceEqualityComparer.Instance.Equals(t1, t2);
        }

        [Fact]
        public void Equals_ReturnsFalse_ValueType()
        {
            Assert.False(ReferenceEqualityComparer.Instance.Equals(42, 42));
        }

        [Fact]
        public void Equals_NullEqualsNull()
        {
            var comparer = ReferenceEqualityComparer.Instance;
            Assert.True(comparer.Equals(null, null));
        }

        [Fact]
        public void GetHashCode_ReturnsSameValueForSameObject()
        {
            var o = new object();
            var comparer = ReferenceEqualityComparer.Instance;
            Assert.Equal(comparer.GetHashCode(o), comparer.GetHashCode(o));
        }

        [Fact]
        public void GetHashCode_DoesNotThrowForNull()
        {
            var comparer = ReferenceEqualityComparer.Instance;

            // Act & Assert (does not throw)
            comparer.GetHashCode(null);
        }

        private class TypeThatOverridesEquals
        {
            public override bool Equals(object obj)
            {
                throw new InvalidOperationException();
            }

            public override int GetHashCode()
            {
                throw new InvalidOperationException();
            }
        }
    }
}