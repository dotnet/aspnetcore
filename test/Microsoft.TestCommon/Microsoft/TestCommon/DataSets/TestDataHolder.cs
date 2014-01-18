// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// Equatable class wrapping a single instance of type <paramref name="T"/>. Equatable to ease test assertions.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to wrap.</typeparam>
    public class TestDataHolder<T> : IEquatable<TestDataHolder<T>>
    {
        public T V1 { get; set; }

        bool IEquatable<TestDataHolder<T>>.Equals(TestDataHolder<T> other)
        {
            if (other == null)
            {
                return false;
            }

            return Object.Equals(V1, other.V1);
        }

        public override bool Equals(object obj)
        {
            TestDataHolder<T> that = obj as TestDataHolder<T>;
            return ((IEquatable<TestDataHolder<T>>)this).Equals(that);
        }

        public override int GetHashCode()
        {
            if (typeof(ValueType).IsAssignableFrom(typeof(T)) || V1 != null)
            {
                return V1.GetHashCode();
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "{{ V1: '{0}' }}", V1);
        }
    }
}
