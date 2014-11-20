// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RouteValueDictionaryTests
    {
        [Fact]
        public void CreateEmpty_UsesOrdinalIgnoreCase()
        {
            // Arrange
            // Act
            var dict = new RouteValueDictionary();

            // Assert
            Assert.Same(StringComparer.OrdinalIgnoreCase, dict.Comparer);
        }

        [Fact]
        public void CreateFromDictionary_UsesOrdinalIgnoreCase()
        {
            // Arrange
            // Act
            var dict = new RouteValueDictionary(new Dictionary<string, object>(StringComparer.Ordinal));

            // Assert
            Assert.Same(StringComparer.OrdinalIgnoreCase, dict.Comparer);
        }

        [Fact]
        public void CreateFromObject_UsesOrdinalIgnoreCase()
        {
            // Arrange
            // Act
            var dict = new RouteValueDictionary(new { cool = "beans" });

            // Assert
            Assert.Same(StringComparer.OrdinalIgnoreCase, dict.Comparer);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromAnonymousType()
        {
            // Arrange
            var obj = new { cool = "beans", awesome = 123 };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.Equal("beans", dict["cool"]);
            Assert.Equal(123, dict["awesome"]);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType()
        {
            // Arrange
            var obj = new RegularType() { CoolnessFactor = 73 };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.Equal(false, dict["IsAwesome"]);
            Assert.Equal(73, dict["CoolnessFactor"]);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_PublicOnly()
        {
            // Arrange
            var obj = new Visibility() { IsPublic = true, ItsInternalDealWithIt = 5 };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(1, dict.Count);
            Assert.Equal(true, dict["IsPublic"]);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_IgnoresStatic()
        {
            // Arrange
            var obj = new StaticProperty();

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(0, dict.Count);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_IgnoresSetOnly()
        {
            // Arrange
            var obj = new SetterOnly() { CoolSetOnly = false };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(0, dict.Count);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_IncludesInherited()
        {
            // Arrange
            var obj = new Derived() { TotallySweetProperty = true, DerivedProperty = false };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.Equal(true, dict["TotallySweetProperty"]);
            Assert.Equal(false, dict["DerivedProperty"]);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_WithHiddenProperty()
        {
            // Arrange
            var obj = new DerivedHiddenProperty() { DerivedProperty = 5 };

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(1, dict.Count);
            Assert.Equal(5, dict["DerivedProperty"]);
        }

        [Fact]
        public void CreateFromObject_CopiesPropertiesFromRegularType_WithIndexerProperty()
        {
            // Arrange
            var obj = new IndexerProperty();

            // Act
            var dict = new RouteValueDictionary(obj);

            // Assert
            Assert.Equal(0, dict.Count);
        }

        [Fact]
        public void CreateFromObject_MixedCaseThrows()
        {
            // Arrange
            var expected = GetDuplicateKeyErrorMessage();
            var obj = new { controller = "Home", Controller = "Home" };

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => new RouteValueDictionary(obj),
                expected);
        }

        private static string GetDuplicateKeyErrorMessage()
        {
            // Gets the exception message when duplicate entries are
            // added to a Dictionary in a platform independent way
            var ex = Assert.Throws<ArgumentException>(
                () => new Dictionary<string, string>()
                {
                    { "key", "value" },
                    { "key", "value" }
                });

            return ex.Message;
        }

        private class RegularType
        {
            public bool IsAwesome { get; set; }

            public int CoolnessFactor { get; set; }
        }

        private class Visibility
        {
            private string PrivateYo { get; set; }

            internal int ItsInternalDealWithIt { get; set; }

            public bool IsPublic { get; set; }
        }

        private class StaticProperty
        {
            public static bool IsStatic { get; set; }
        }

        private class SetterOnly
        {
            private bool _coolSetOnly;

            public bool CoolSetOnly { set { _coolSetOnly = value; } }
        }

        private class Base
        {
            public bool DerivedProperty { get; set; }
        }

        private class Derived : Base
        {
            public bool TotallySweetProperty { get; set; }
        }

        private class DerivedHiddenProperty : Base
        {
            public new int DerivedProperty { get; set; }
        }

        private class IndexerProperty
        {
            public bool this[string key]
            {
                get { return false; }
                set { }
            }
        }
    }
}
