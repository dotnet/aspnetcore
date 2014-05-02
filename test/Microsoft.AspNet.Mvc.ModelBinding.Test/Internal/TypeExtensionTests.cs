// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
        [MemberData("TypesWithValues")]
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
