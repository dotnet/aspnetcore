// Copyright (c) .NET Foundation. All rights reserved.
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

        [Fact]
        public void CreateFromReadOnlyDictionary_CopiesValues()
        {
            // Arrange
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("Name", "James");
            dictionary.Add("Age", 30);
            var readonlyDictionary = (IReadOnlyDictionary<string, object>)dictionary;

            // Act
            var dict = new RouteValueDictionary(readonlyDictionary);

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.Equal("James", dict["Name"]);
            Assert.Equal(30, dict["Age"]);
        }

        [Fact]
        public void CreateFromReadOnlyDictionary_ModifyRouteValueDictionary()
        {
            // Arrange
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("Name", "James");
            dictionary.Add("Age", 30);
            dictionary.Add("Address", new Address() { City = "Redmond", State = "WA" });
            var readonlyDictionary = (IReadOnlyDictionary<string, object>)dictionary;

            // Act
            var routeValueDictionary = new RouteValueDictionary(readonlyDictionary);
            routeValueDictionary.Add("City", "Redmond");

            // Assert
            Assert.Equal(4, routeValueDictionary.Count);
            Assert.Equal("James", routeValueDictionary["Name"]);
            Assert.Equal(30, routeValueDictionary["Age"]);
            Assert.Equal("Redmond", routeValueDictionary["City"]);
            var address = Assert.IsType<Address>(routeValueDictionary["Address"]);
            address.State = "Washington";
            Assert.Equal("Washington", ((Address)routeValueDictionary["Address"]).State);
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

        private class Address
        {
            public string City { get; set; }

            public string State { get; set; }
        }
    }
}
