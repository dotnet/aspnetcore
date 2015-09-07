// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.Primitives
{
    public class StringValuesTests
    {
        public static TheoryData<StringValues> DefaultOrNullStringValues
        {
            get
            {
                return new TheoryData<StringValues>
                {
                    new StringValues(),
                    new StringValues((string)null),
                    new StringValues((string[])null),
                    (string)null,
                    (string[])null
                };
            }
        }

        public static TheoryData<StringValues> EmptyStringValues
        {
            get
            {
                return new TheoryData<StringValues>
                {
                    StringValues.Empty,
                    new StringValues(new string[0]),
                    new string[0]
                };
            }
        }

        public static TheoryData<StringValues> FilledStringValues
        {
            get
            {
                return new TheoryData<StringValues>
                {
                    new StringValues("abc"),
                    new StringValues(new[] { "abc" }),
                    new StringValues(new[] { "abc", "bcd" }),
                    new StringValues(new[] { "abc", "bcd", "foo" }),
                    "abc",
                    new[] { "abc" },
                    new[] { "abc", "bcd" },
                    new[] { "abc", "bcd", "foo" }
                };
            }
        }

        public static TheoryData<StringValues, string[]> FilledStringValuesWithExpected
        {
            get
            {
                return new TheoryData<StringValues, string[]>
                {
                    { new StringValues("abc"), new[] { "abc" } },
                    { new StringValues(new[] { "abc" }), new[] { "abc" } },
                    { new StringValues(new[] { "abc", "bcd" }), new[] { "abc", "bcd" } },
                    { new StringValues(new[] { "abc", "bcd", "foo" }), new[] { "abc", "bcd", "foo" } },
                    { "abc", new[] { "abc" } },
                    { new[] { "abc" }, new[] { "abc" } },
                    { new[] { "abc", "bcd" }, new[] { "abc", "bcd" } },
                    { new[] { "abc", "bcd", "foo" }, new[] { "abc", "bcd", "foo" } }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DefaultOrNullStringValues))]
        [MemberData(nameof(EmptyStringValues))]
        [MemberData(nameof(FilledStringValues))]
        public void IsReadOnly_True(StringValues stringValues)
        {
            Assert.True(((IList<string>)stringValues).IsReadOnly);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues)[0] = string.Empty);
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Add(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).Insert(0, string.Empty));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Remove(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Clear());
        }

        [Theory]
        [MemberData(nameof(DefaultOrNullStringValues))]
        public void DefaultOrNull_ExpectedValues(StringValues stringValues)
        {
            Assert.Null((string[])stringValues);
        }

        [Theory]
        [MemberData(nameof(DefaultOrNullStringValues))]
        [MemberData(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_ExpectedValues(StringValues stringValues)
        {
            Assert.Equal(0, stringValues.Count);
            Assert.Null((string)stringValues);
            Assert.Equal((string)null, stringValues);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(new string[0], stringValues.ToArray());

            Assert.True(StringValues.IsNullOrEmpty(stringValues));
            Assert.Throws<IndexOutOfRangeException>(() => stringValues[0]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList<string>)stringValues)[0]);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(null));
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(string.Empty));
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf("not there"));
            Assert.False(((ICollection<string>)stringValues).Contains(null));
            Assert.False(((ICollection<string>)stringValues).Contains(string.Empty));
            Assert.False(((ICollection<string>)stringValues).Contains("not there"));
            Assert.Equal(0, stringValues.Count());
        }

        [Fact]
        public void ImplicitStringConverter_Works()
        {
            string nullString = null;
            StringValues stringValues = nullString;
            Assert.Equal(0, stringValues.Count);
            Assert.Null((string)stringValues);
            Assert.Null((string[])stringValues);

            string aString = "abc";
            stringValues = aString;
            Assert.Equal(1, stringValues.Count);
            Assert.Equal(aString, stringValues);
            Assert.Equal(aString, stringValues[0]);
            Assert.Equal(aString, ((IList<string>)stringValues)[0]);
            Assert.Equal<string[]>(new string[] { aString }, stringValues);
        }

        [Fact]
        public void ImplicitStringArrayConverter_Works()
        {
            string[] nullStringArray = null;
            StringValues stringValues = nullStringArray;
            Assert.Equal(0, stringValues.Count);
            Assert.Null((string)stringValues);
            Assert.Null((string[])stringValues);

            string aString = "abc";
            string[] aStringArray = new[] { aString };
            stringValues = aStringArray;
            Assert.Equal(1, stringValues.Count);
            Assert.Equal(aString, stringValues);
            Assert.Equal(aString, stringValues[0]);
            Assert.Equal(aString, ((IList<string>)stringValues)[0]);
            Assert.Equal<string[]>(aStringArray, stringValues);

            aString = "abc";
            string bString = "bcd";
            aStringArray = new[] { aString, bString };
            stringValues = aStringArray;
            Assert.Equal(2, stringValues.Count);
            Assert.Equal("abc,bcd", stringValues);
            Assert.Equal<string[]>(aStringArray, stringValues);
        }

        [Theory]
        [MemberData(nameof(DefaultOrNullStringValues))]
        [MemberData(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_Enumerator(StringValues stringValues)
        {
            var e = stringValues.GetEnumerator();
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.Null(e.Current);
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)stringValues).GetEnumerator();
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.Null(e1.Current);
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)stringValues).GetEnumerator();
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.Null(e2.Current);
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [MemberData(nameof(FilledStringValuesWithExpected))]
        public void Enumerator(StringValues stringValues, string[] expected)
        {
            var e = stringValues.GetEnumerator();
            Assert.Null(e.Current);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e.MoveNext());
                Assert.Equal(expected[i], e.Current);
            }
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());
            Assert.False(e.MoveNext());

            var e1 = ((IEnumerable<string>)stringValues).GetEnumerator();
            Assert.Null(e1.Current);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e1.MoveNext());
                Assert.Equal(expected[i], e1.Current);
            }
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());
            Assert.False(e1.MoveNext());

            var e2 = ((IEnumerable)stringValues).GetEnumerator();
            Assert.Null(e2.Current);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(e2.MoveNext());
                Assert.Equal(expected[i], e2.Current);
            }
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
            Assert.False(e2.MoveNext());
        }

        [Theory]
        [MemberData(nameof(FilledStringValuesWithExpected))]
        public void IndexOf(StringValues stringValues, string[] expected)
        {
            IList<string> list = stringValues;
            Assert.Equal(-1, list.IndexOf("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(expected[i]));
            }
        }

        [Theory]
        [MemberData(nameof(FilledStringValuesWithExpected))]
        public void Contains(StringValues stringValues, string[] expected)
        {
            ICollection<string> collection = stringValues;
            Assert.False(collection.Contains("not there"));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(collection.Contains(expected[i]));
            }
        }

        [Theory]
        [MemberData(nameof(FilledStringValuesWithExpected))]
        public void CopyTo(StringValues stringValues, string[] expected)
        {
            ICollection<string> collection = stringValues;

            string[] tooSmall = new string[0];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(tooSmall, 0));

            string[] actual = new string[expected.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(actual, -1));
            Assert.Throws<ArgumentException>(() => collection.CopyTo(actual, actual.Length + 1));
            collection.CopyTo(actual, 0);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(DefaultOrNullStringValues))]
        [MemberData(nameof(EmptyStringValues))]
        public void DefaultNullOrEmpty_Concat(StringValues stringValues)
        {
            string[] expected = new[] { "abc", "bcd", "foo" };
            Assert.Equal(expected, StringValues.Concat(stringValues, new StringValues(expected)));
            Assert.Equal(expected, StringValues.Concat(new StringValues(expected), stringValues));

            string[] empty = new string[0];
            Assert.Equal(empty, StringValues.Concat(stringValues, StringValues.Empty));
            Assert.Equal(empty, StringValues.Concat(StringValues.Empty, stringValues));
            Assert.Equal(empty, StringValues.Concat(stringValues, new StringValues()));
            Assert.Equal(empty, StringValues.Concat(new StringValues(), stringValues));
        }

        [Theory]
        [MemberData(nameof(FilledStringValuesWithExpected))]
        public void Concat(StringValues stringValues, string[] array)
        {
            string[] filled = new[] { "abc", "bcd", "foo" };

            string[] expectedPrepended = array.Concat(filled).ToArray();
            Assert.Equal(expectedPrepended, StringValues.Concat(stringValues, new StringValues(filled)));

            string[] expectedAppended = filled.Concat(array).ToArray();
            Assert.Equal(expectedAppended, StringValues.Concat(new StringValues(filled), stringValues));
        }
    }
}
