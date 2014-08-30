// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class TypeExtensionTests
    {
        [Theory]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(Guid))]
        public void IsCompatibleWithReturnsFalse_IfValueTypeIsNull(Type type)
        {
            // Act
            bool result = TypeExtensions.IsCompatibleWith(type, value: null);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(short))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(Foo))]
        public void IsCompatibleWithReturnsFalse_IfValueIsMismatched(Type type)
        {
            // Act
            bool result = TypeExtensions.IsCompatibleWith(type, value: "Hello world");

            // Assert
            Assert.False(result);
        }

        public static IEnumerable<object[]> TypesWithValues
        {
            get
            {
                yield return new object[] { typeof(int?), null };
                yield return new object[] { typeof(int), 4 };
                yield return new object[] { typeof(int?), 1 };
                yield return new object[] { typeof(DateTime?), null };
                yield return new object[] { typeof(Guid), Guid.Empty };
                yield return new object[] { typeof(DateTimeOffset?), DateTimeOffset.UtcNow };
                yield return new object[] { typeof(string), null };
                yield return new object[] { typeof(string), "foo string" };
                yield return new object[] { typeof(Foo), null };
                yield return new object[] { typeof(Foo), new Foo() };
            }
        }

        [Theory]
        [MemberData(nameof(TypesWithValues))]
        public void IsCompatibleWithReturnsTrue_IfValueIsAssignable(Type type, object value)
        {
            // Act
            bool result = TypeExtensions.IsCompatibleWith(type, value);

            // Assert
            Assert.True(result);
        }

        private class Foo
        {
        }
    }
}
