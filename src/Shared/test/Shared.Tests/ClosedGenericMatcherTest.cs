// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class ClosedGenericMatcherTest
{
    // queryType, interfaceType, expectedResult
    public static TheoryData<Type, Type, Type> ExtractGenericInterfaceDataSet
    {
        get
        {
            return new TheoryData<Type, Type, Type>
                {
                    // Closed generic types that match given open generic type.
                    {
                        typeof(IEnumerable<BaseClass>),
                        typeof(IEnumerable<>),
                        typeof(IEnumerable<BaseClass>)
                    },
                    {
                        typeof(IReadOnlyList<int>),
                        typeof(IReadOnlyList<>),
                        typeof(IReadOnlyList<int>)
                    },
                    {
                        typeof(KeyValuePair<string, object>),
                        typeof(KeyValuePair<,>),
                        typeof(KeyValuePair<string, object>)
                    },
                    // Closed generic interfaces that implement sub-interface of given open generic type.
                    {
                        typeof(ICollection<BaseClass>),
                        typeof(IEnumerable<>),
                        typeof(IEnumerable<BaseClass>)
                    },
                    {
                        typeof(IReadOnlyList<int>),
                        typeof(IEnumerable<>),
                        typeof(IEnumerable<int>)
                    },
                    {
                        typeof(IDictionary<string, object>),
                        typeof(IEnumerable<>),
                        typeof(IEnumerable<KeyValuePair<string, object>>)
                    },
                    // Class that implements closed generic based on given open generic interface.
                    {
                        typeof(BaseClass),
                        typeof(IDictionary<,>),
                        typeof(IDictionary<string, object>)
                    },
                    {
                        typeof(BaseClass),
                        typeof(IEquatable<>),
                        typeof(IEquatable<BaseClass>)
                    },
                    {
                        typeof(BaseClass),
                        typeof(ICollection<>),
                        typeof(ICollection<KeyValuePair<string, object>>)
                    },
                    // Derived class that implements closed generic based on given open generic interface.
                    {
                        typeof(DerivedClass),
                        typeof(IDictionary<,>),
                        typeof(IDictionary<string, object>)
                    },
                    {
                        typeof(DerivedClass),
                        typeof(IEquatable<>),
                        typeof(IEquatable<BaseClass>)
                    },
                    {
                        typeof(DerivedClass),
                        typeof(ICollection<>),
                        typeof(ICollection<KeyValuePair<string, object>>)
                    },
                    // Derived class that also implements another interface.
                    {
                        typeof(DerivedClassWithComparable),
                        typeof(IDictionary<,>),
                        typeof(IDictionary<string, object>)
                    },
                    {
                        typeof(DerivedClassWithComparable),
                        typeof(IEquatable<>),
                        typeof(IEquatable<BaseClass>)
                    },
                    {
                        typeof(DerivedClassWithComparable),
                        typeof(ICollection<>),
                        typeof(ICollection<KeyValuePair<string, object>>)
                    },
                    {
                        typeof(DerivedClassWithComparable),
                        typeof(IComparable<>),
                        typeof(IComparable<DerivedClassWithComparable>)
                    },
                    // Derived class using system implementation.
                    {
                        typeof(DerivedClassFromSystemImplementation),
                        typeof(ICollection<>),
                        typeof(ICollection<BaseClass>)
                    },
                    {
                        typeof(DerivedClassFromSystemImplementation),
                        typeof(IReadOnlyList<>),
                        typeof(IReadOnlyList<BaseClass>)
                    },
                    {
                        typeof(DerivedClassFromSystemImplementation),
                        typeof(IEnumerable<>),
                        typeof(IEnumerable<BaseClass>)
                    },
                    // Not given an open generic type.
                    {
                        typeof(IEnumerable<BaseClass>),
                        typeof(IEnumerable<BaseClass>),
                        null
                    },
                    {
                        typeof(IEnumerable<BaseClass>),
                        typeof(IEnumerable),
                        null
                    },
                    {
                        typeof(IReadOnlyList<int>),
                        typeof(BaseClass),
                        null
                    },
                    {
                        typeof(KeyValuePair<,>),
                        typeof(KeyValuePair<string, object>),
                        null
                    },
                    // Not a match.
                    {
                        typeof(IEnumerable<BaseClass>),
                        typeof(IReadOnlyList<>),
                        null
                    },
                    {
                        typeof(IList<int>),
                        typeof(IReadOnlyList<>),
                        null
                    },
                    {
                        typeof(IDictionary<string, object>),
                        typeof(KeyValuePair<,>),
                        null
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExtractGenericInterfaceDataSet))]
    public void ExtractGenericInterface_ReturnsExpectedType(
        Type queryType,
        Type interfaceType,
        Type expectedResult)
    {
        // Arrange & Act
        var result = ClosedGenericMatcher.ExtractGenericInterface(queryType, interfaceType);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    // IEnumerable<int> is preferred because it is defined on the more-derived type.
    [Fact]
    public void ExtractGenericInterface_MultipleDefinitionsInherited()
    {
        // Arrange
        var type = typeof(TwoIEnumerableImplementationsInherited);

        // Act
        var result = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));

        // Sort
        Assert.Equal(typeof(IEnumerable<int>), result);
    }

    // IEnumerable<int> is preferred because we sort by Ordinal on the full name.
    [Fact]
    public void ExtractGenericInterface_MultipleDefinitionsOnSameType()
    {
        // Arrange
        var type = typeof(TwoIEnumerableImplementationsOnSameClass);

        // Act
        var result = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IEnumerable<>));

        // Sort
        Assert.Equal(typeof(IEnumerable<int>), result);
    }

    private class TwoIEnumerableImplementationsOnSameClass : IEnumerable<string>, IEnumerable<int>
    {
        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    private class TwoIEnumerableImplementationsInherited : List<int>, IEnumerable<string>
    {
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    private class BaseClass : IDictionary<string, object>, IEquatable<BaseClass>
    {
        object IDictionary<string, object>.this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Equals(BaseClass other)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedClass : BaseClass
    {
    }

    private class DerivedClassWithComparable : DerivedClass, IComparable<DerivedClassWithComparable>
    {
        public int CompareTo(DerivedClassWithComparable other)
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedClassFromSystemImplementation : Collection<BaseClass>
    {
    }
}
