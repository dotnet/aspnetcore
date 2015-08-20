// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.Primitives
{
    public class StringValuesTests
    {
        [Fact]
        public void IsReadOnly_True()
        {
            var stringValues = new StringValues();
            Assert.True(((IList<string>)stringValues).IsReadOnly);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues)[0] = string.Empty);
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Add(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).Insert(0, string.Empty));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Remove(string.Empty));
            Assert.Throws<NotSupportedException>(() => ((IList<string>)stringValues).RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)stringValues).Clear());
        }

        [Fact]
        public void DefaultConstructor_ExpectedValues()
        {
            var stringValues = new StringValues();
            Assert.Equal(0, stringValues.Count);
            Assert.Equal((string)null, stringValues);
            Assert.Equal(new string[0], stringValues.ToArray());

            Assert.True(StringValues.IsNullOrEmpty(stringValues));
            Assert.Throws<IndexOutOfRangeException>(() => stringValues[0]);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(string.Empty));
            Assert.Equal(0, stringValues.Count());
        }

        [Fact]
        public void Constructor_NullStringValue_ExpectedValues()
        {
            var stringValues = new StringValues((string)null);
            Assert.Equal(0, stringValues.Count);
            Assert.Null((string)stringValues);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Null((string[])stringValues);
            Assert.Equal(new string[0], stringValues.ToArray());

            Assert.True(StringValues.IsNullOrEmpty(stringValues));
            Assert.Throws<IndexOutOfRangeException>(() => stringValues[0]);
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(string.Empty));
            Assert.Equal(0, stringValues.Count());
        }

        [Fact]
        public void Constructor_NullStringArray_ExpectedValues()
        {
            var stringValues = new StringValues((string[])null);
            Assert.Equal(0, stringValues.Count);
            Assert.Null((string)stringValues);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Null((string[])stringValues);
            Assert.Equal(new string[0], stringValues.ToArray());

            Assert.True(StringValues.IsNullOrEmpty(stringValues));
            Assert.Throws<IndexOutOfRangeException>(() => stringValues[0]);
            Assert.Equal(string.Empty, stringValues.ToString());
            Assert.Equal(-1, ((IList<string>)stringValues).IndexOf(string.Empty));
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
            Assert.Equal<string[]>(aStringArray, stringValues);

            aString = "abc";
            string bString = "bcd";
            aStringArray = new[] { aString, bString };
            stringValues = aStringArray;
            Assert.Equal(2, stringValues.Count);
            Assert.Equal("abc,bcd", stringValues);
            Assert.Equal<string[]>(aStringArray, stringValues);
        }
    }
}
