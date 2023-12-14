// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Routing.Tests;

public class RouteValueDictionaryTests
{
    [Fact]
    public void DefaultCtor_UsesEmptyStorage()
    {
        // Arrange
        // Act
        var dict = new RouteValueDictionary();

        // Assert
        Assert.Empty(dict);
        Assert.Empty(dict._arrayStorage);
        Assert.Null(dict._propertyStorage);
    }

    [Fact]
    public void CreateFromNull_UsesEmptyStorage()
    {
        // Arrange
        // Act
        var dict = new RouteValueDictionary(values: (object?)null);

        // Assert
        Assert.Empty(dict);
        Assert.Empty(dict._arrayStorage);
        Assert.Null(dict._propertyStorage);
    }

    [Fact]
    public void CreateFromRouteValueDictionary_WithArrayStorage_CopiesStorage()
    {
        // Arrange
        var other = new RouteValueDictionary()
            {
                { "1", 1 }
            };

        // Act
        var dict = new RouteValueDictionary(other);

        // Assert
        Assert.Equal(other, dict);
        Assert.Single(dict._arrayStorage);
        Assert.Null(dict._propertyStorage);

        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        var otherStorage = Assert.IsType<KeyValuePair<string, object?>[]>(other._arrayStorage);
        Assert.NotSame(otherStorage, storage);
    }

    [Fact]
    public void CreateFromRouteValueDictionary_WithPropertyStorage_CopiesStorage()
    {
        // Arrange
        var other = new RouteValueDictionary(new { key = "value" });

        // Act
        var dict = new RouteValueDictionary(other);

        // Assert
        Assert.Equal(other, dict);
        AssertEmptyArrayStorage(dict);

        var storage = dict._propertyStorage;
        var otherStorage = other._propertyStorage;
        Assert.Same(otherStorage, storage);
    }

    public static IEnumerable<object[]> IEnumerableKeyValuePairData
    {
        get
        {
            var routeValues = new[]
            {
                    new KeyValuePair<string, object?>("Name", "James"),
                    new KeyValuePair<string, object?>("Age", 30),
                    new KeyValuePair<string, object?>("Address", new Address() { City = "Redmond", State = "WA" })
                };

            yield return new object[] { routeValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) };

            yield return new object[] { routeValues.ToList() };

            yield return new object[] { routeValues };
        }
    }

    public static IEnumerable<object[]> IEnumerableStringValuePairData
    {
        get
        {
            var routeValues = new[]
            {
                    new KeyValuePair<string, string>("First Name", "James"),
                    new KeyValuePair<string, string>("Last Name", "Henrik"),
                    new KeyValuePair<string, string>("Middle Name", "Bob")
                };

            yield return new object[] { routeValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) };

            yield return new object[] { routeValues.ToList() };

            yield return new object[] { routeValues };
        }
    }

    [Theory]
    [MemberData(nameof(IEnumerableKeyValuePairData))]
    public void CreateFromIEnumerableKeyValuePair_CopiesValues(object values)
    {
        // Arrange & Act
        var dict = new RouteValueDictionary(values);

        // Assert
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("Address", kvp.Key);
                var address = Assert.IsType<Address>(kvp.Value);
                Assert.Equal("Redmond", address.City);
                Assert.Equal("WA", address.State);
            },
            kvp => { Assert.Equal("Age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("Name", kvp.Key); Assert.Equal("James", kvp.Value); });
    }

    [Theory]
    [MemberData(nameof(IEnumerableStringValuePairData))]
    public void CreateFromIEnumerableStringValuePair_CopiesValues(object values)
    {
        // Arrange & Act
        var dict = new RouteValueDictionary(values);

        // Assert
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("First Name", kvp.Key); Assert.Equal("James", kvp.Value); },
            kvp => { Assert.Equal("Last Name", kvp.Key); Assert.Equal("Henrik", kvp.Value); },
            kvp => { Assert.Equal("Middle Name", kvp.Key); Assert.Equal("Bob", kvp.Value); });
    }

    [Fact]
    public void CreateFromIEnumerableKeyValuePair_ThrowsExceptionForDuplicateKey()
    {
        // Arrange
        var values = new List<KeyValuePair<string, object?>>()
            {
                new KeyValuePair<string, object?>("name", "Billy"),
                new KeyValuePair<string, object?>("Name", "Joey"),
            };

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new RouteValueDictionary(values),
            "key",
            $"An element with the key 'Name' already exists in the {nameof(RouteValueDictionary)}.");
    }

    [Fact]
    public void CreateFromIEnumerableStringValuePair_ThrowsExceptionForDuplicateKey()
    {
        // Arrange
        var values = new List<KeyValuePair<string, string?>>()
        {
            new("name", "Billy"),
            new("Name", "Joey"),
        };

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new RouteValueDictionary(values),
            "key",
            $"An element with the key 'Name' already exists in the {nameof(RouteValueDictionary)}.");
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromAnonymousType()
    {
        // Arrange
        var obj = new { cool = "beans", awesome = 123 };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("awesome", kvp.Key); Assert.Equal(123, kvp.Value); },
            kvp => { Assert.Equal("cool", kvp.Key); Assert.Equal("beans", kvp.Value); });
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType()
    {
        // Arrange
        var obj = new RegularType() { CoolnessFactor = 73 };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("CoolnessFactor", kvp.Key);
                Assert.Equal(73, kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("IsAwesome", kvp.Key);
                var value = Assert.IsType<bool>(kvp.Value);
                Assert.False(value);
            });
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_PublicOnly()
    {
        // Arrange
        var obj = new Visibility() { IsPublic = true, ItsInternalDealWithIt = 5 };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("IsPublic", kvp.Key);
                var value = Assert.IsType<bool>(kvp.Value);
                Assert.True(value);
            });
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_IgnoresStatic()
    {
        // Arrange
        var obj = new StaticProperty();

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_IgnoresSetOnly()
    {
        // Arrange
        var obj = new SetterOnly() { CoolSetOnly = false };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_IncludesInherited()
    {
        // Arrange
        var obj = new Derived() { TotallySweetProperty = true, DerivedProperty = false };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("DerivedProperty", kvp.Key);
                var value = Assert.IsType<bool>(kvp.Value);
                Assert.False(value);
            },
            kvp =>
            {
                Assert.Equal("TotallySweetProperty", kvp.Key);
                var value = Assert.IsType<bool>(kvp.Value);
                Assert.True(value);
            });
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_WithHiddenProperty()
    {
        // Arrange
        var obj = new DerivedHiddenProperty() { DerivedProperty = 5 };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("DerivedProperty", kvp.Key); Assert.Equal(5, kvp.Value); });
    }

    [Fact]
    public void CreateFromObject_CopiesPropertiesFromRegularType_WithIndexerProperty()
    {
        // Arrange
        var obj = new IndexerProperty();

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Empty(dict);
    }

    [Fact]
    public void CreateFromObject_MixedCaseThrows()
    {
        // Arrange
        var obj = new { controller = "Home", Controller = "Home" };

        var message =
            $"The type '{obj.GetType().FullName}' defines properties 'controller' and 'Controller' which differ " +
            $"only by casing. This is not supported by {nameof(RouteValueDictionary)} which uses " +
            $"case-insensitive comparisons.";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var dictionary = new RouteValueDictionary(obj);
        });

        // Ignoring case to make sure we're not testing reflection's ordering.
        Assert.Equal(message, exception.Message, ignoreCase: true);
    }

    [Fact]
    public void CreateFromObject_Struct_ReadValues()
    {
        // Arrange
        var obj = new StructAddress() { City = "Singapore" };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("City", kvp.Key); Assert.Equal("Singapore", kvp.Value); },
            kvp => { Assert.Equal("State", kvp.Key); Assert.Null(kvp.Value); });
    }

    [Fact]
    public void CreateFromObject_NullableStruct_ReadValues()
    {
        // Arrange
        StructAddress? obj = new StructAddress() { City = "Singapore" };

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("City", kvp.Key); Assert.Equal("Singapore", kvp.Value); },
            kvp => { Assert.Equal("State", kvp.Key); Assert.Null(kvp.Value); });
    }

    [Fact]
    public void CreateFromObject_NullStruct_ReadValues()
    {
        // Arrange
        StructAddress? obj = null;

        // Act
        var dict = new RouteValueDictionary(obj);

        // Assert
        Assert.Null(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Empty(dict);
    }

    // Our comparer is hardcoded to be OrdinalIgnoreCase no matter what.
    [Fact]
    public void Comparer_IsOrdinalIgnoreCase()
    {
        // Arrange
        // Act
        var dict = new RouteValueDictionary();

        // Assert
        Assert.Same(StringComparer.OrdinalIgnoreCase, dict.Comparer);
    }

    // Our comparer is hardcoded to be IsReadOnly==false no matter what.
    [Fact]
    public void IsReadOnly_False()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).IsReadOnly;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IndexGet_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var value = dict[""];

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void IndexGet_EmptyStorage_ReturnsNull()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var value = dict["key"];

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void IndexGet_PropertyStorage_NoMatch_ReturnsNull()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { age = 30 });

        // Act
        var value = dict["key"];

        // Assert
        Assert.Null(value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void IndexGet_PropertyStorage_Match_ReturnsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var value = dict["key"];

        // Assert
        Assert.Equal("value", value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void IndexGet_PropertyStorage_MatchIgnoreCase_ReturnsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var value = dict["kEy"];

        // Assert
        Assert.Equal("value", value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void IndexGet_ArrayStorage_NoMatch_ReturnsNull()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "age", 30 },
            };

        // Act
        var value = dict["key"];

        // Assert
        Assert.Null(value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexGet_ListStorage_Match_ReturnsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var value = dict["key"];

        // Assert
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexGet_ListStorage_MatchIgnoreCase_ReturnsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var value = dict["kEy"];

        // Assert
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        dict[""] = "foo";

        // Assert
        Assert.Equal("foo", dict[""]);
    }

    [Fact]
    public void IndexSet_EmptyStorage_UpgradesToList()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_PropertyStorage_NoMatch_AddsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { age = 30 });

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_PropertyStorage_Match_SetsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_PropertyStorage_MatchIgnoreCase_SetsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        dict["kEy"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("kEy", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_NoMatch_AddsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "age", 30 },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_Match_SetsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void IndexSet_ListStorage_MatchIgnoreCase_SetsValue()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        dict["key"] = "value";

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Count_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var count = dict.Count;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Count_PropertyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value", });

        // Act
        var count = dict.Count;

        // Assert
        Assert.Equal(1, count);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void Count_ListStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var count = dict.Count;

        // Assert
        Assert.Equal(1, count);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Keys_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var keys = dict.Keys;

        // Assert
        Assert.Empty(keys);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Keys_PropertyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value", });

        // Act
        var keys = dict.Keys;

        // Assert
        Assert.Equal(new[] { "key" }, keys);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Keys_ListStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var keys = dict.Keys;

        // Assert
        Assert.Equal(new[] { "key" }, keys);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Values_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var values = dict.Values;

        // Assert
        Assert.Empty(values);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Values_PropertyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value", });

        // Act
        var values = dict.Values;

        // Assert
        Assert.Equal(new object[] { "value" }, values);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Values_ListStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var values = dict.Values;

        // Assert
        Assert.Equal(new object[] { "value" }, values);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        dict.Add("key", "value");

        // Assert
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        dict.Add("", "foo");

        // Assert
        Assert.Equal("foo", dict[""]);
    }

    [Fact]
    public void Add_PropertyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { age = 30 });

        // Act
        dict.Add("key", "value");

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);

        // The upgrade from property -> array should make space for at least 4 entries
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("age", 30), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void Add_ListStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "age", 30 },
            };

        // Act
        dict.Add("key", "value");

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_DuplicateKey()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var message = $"An element with the key 'key' already exists in the {nameof(RouteValueDictionary)}";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(() => dict.Add("key", "value2"), "key", message);

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_DuplicateKey_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var message = $"An element with the key 'kEy' already exists in the {nameof(RouteValueDictionary)}";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(() => dict.Add("kEy", "value2"), "key", message);

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Add_KeyValuePair()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "age", 30 },
            };

        // Act
        ((ICollection<KeyValuePair<string, object?>>)dict).Add(new KeyValuePair<string, object?>("key", "value"));

        // Assert
        Assert.Collection(
            dict.OrderBy(kvp => kvp.Key),
            kvp => { Assert.Equal("age", kvp.Key); Assert.Equal(30, kvp.Value); },
            kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Clear_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
    }

    [Fact]
    public void Clear_PropertyStorage_AlreadyEmpty()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { });

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
    }

    [Fact]
    public void Clear_PropertyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
        Assert.Null(dict._propertyStorage);
        Assert.Empty(dict._arrayStorage);
    }

    [Fact]
    public void Clear_ListStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        dict.Clear();

        // Assert
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Null(dict._propertyStorage);
    }

    [Fact]
    public void Contains_ListStorage_KeyValuePair_True()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Contains_ListStory_KeyValuePair_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("KEY", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Contains_ListStorage_KeyValuePair_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("other", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    // Value comparisons use the default equality comparer.
    [Fact]
    public void Contains_ListStorage_KeyValuePair_False_ValueComparisonIsDefault()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "valUE");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Contains_PropertyStorage_KeyValuePair_True()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        var input = new KeyValuePair<string, object?>("key", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp));
    }

    [Fact]
    public void Contains_PropertyStory_KeyValuePair_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        var input = new KeyValuePair<string, object?>("KEY", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.True(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp));
    }

    [Fact]
    public void Contains_PropertyStorage_KeyValuePair_False()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        var input = new KeyValuePair<string, object?>("other", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp));
    }

    // Value comparisons use the default equality comparer.
    [Fact]
    public void Contains_PropertyStorage_KeyValuePair_False_ValueComparisonIsDefault()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        var input = new KeyValuePair<string, object?>("key", "valUE");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Contains(input);

        // Assert
        Assert.False(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
        Assert.Collection(
            dict,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp));
    }

    [Fact]
    public void ContainsKey_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.ContainsKey("key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsKey_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.ContainsKey("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsKey_PropertyStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.ContainsKey("other");

        // Assert
        Assert.False(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
    }

    [Fact]
    public void ContainsKey_PropertyStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.ContainsKey("key");

        // Assert
        Assert.True(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
    }

    [Fact]
    public void ContainsKey_PropertyStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.ContainsKey("kEy");

        // Assert
        Assert.True(result);
        Assert.NotNull(dict._propertyStorage);
        AssertEmptyArrayStorage(dict);
    }

    [Fact]
    public void ContainsKey_ListStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("other");

        // Assert
        Assert.False(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ContainsKey_ListStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("key");

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ContainsKey_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.ContainsKey("kEy");

        // Assert
        Assert.True(result);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void CopyTo()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var array = new KeyValuePair<string, object?>[2];

        // Act
        ((ICollection<KeyValuePair<string, object?>>)dict).CopyTo(array, 1);

        // Assert
        Assert.Equal(
            new KeyValuePair<string, object?>[]
            {
                    default(KeyValuePair<string, object?>),
                    new KeyValuePair<string, object?>("key", "value")
            },
            array);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_True()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("KEY", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyValuePair_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("other", "value");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    // Value comparisons use the default equality comparer.
    [Fact]
    public void Remove_KeyValuePair_False_ValueComparisonIsDefault()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        var input = new KeyValuePair<string, object?>("key", "valUE");

        // Act
        var result = ((ICollection<KeyValuePair<string, object?>>)dict).Remove(input);

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.Remove("key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.Remove("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Remove_PropertyStorage_Empty()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { });

        // Act
        var result = dict.Remove("other");

        // Assert
        Assert.False(result);
        Assert.Empty(dict);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void Remove_PropertyStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.Remove("other");

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_PropertyStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.Remove("key");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_PropertyStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.Remove("kEy");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_ListStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("other");

        // Assert
        Assert.False(result);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_ListStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("key");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("kEy");

        // Assert
        Assert.True(result);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
    }

    [Fact]
    public void Remove_KeyAndOutValue_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.Remove("", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
    }

    [Fact]
    public void Remove_KeyAndOutValue_PropertyStorage_Empty()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { });

        // Act
        var result = dict.Remove("other", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
        Assert.Empty(dict);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_PropertyStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.Remove("other", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_PropertyStorage_True()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary(new { key = value });

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_PropertyStorage_True_CaseInsensitive()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary(new { key = value });

        // Act
        var result = dict.Remove("kEy", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.Remove("other", out var removedValue);

        // Assert
        Assert.False(result);
        Assert.Null(removedValue);
        Assert.Collection(dict, kvp => { Assert.Equal("key", kvp.Key); Assert.Equal("value", kvp.Value); });
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_True()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary()
            {
                { "key", value }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary()
            {
                { "key", value }
            };

        // Act
        var result = dict.Remove("kEy", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Empty(dict);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_First()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary()
            {
                { "key", value },
                { "other", 5 },
                { "dotnet", "rocks" }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_Middle()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary()
            {
                { "other", 5 },
                { "key", value },
                { "dotnet", "rocks" }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void Remove_KeyAndOutValue_ListStorage_KeyExists_Last()
    {
        // Arrange
        object value = "value";
        var dict = new RouteValueDictionary()
            {
                { "other", 5 },
                { "dotnet", "rocks" },
                { "key", value }
            };

        // Act
        var result = dict.Remove("key", out var removedValue);

        // Assert
        Assert.True(result);
        Assert.Same(value, removedValue);
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey("key"));
        Assert.True(dict.ContainsKey("other"));
        Assert.True(dict.ContainsKey("dotnet"));
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryAdd_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.TryAdd("", "foo");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryAdd_PropertyStorage_KeyDoesNotExist_ConvertsPropertyStorageToArrayStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value", });

        // Act
        var result = dict.TryAdd("otherKey", "value");

        // Assert
        Assert.True(result);
        Assert.Null(dict._propertyStorage);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("otherKey", "value"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_PropertyStory_KeyExist_DoesNotConvertPropertyStorageToArrayStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value", });

        // Act
        var result = dict.TryAdd("key", "value");

        // Assert
        Assert.False(result);
        AssertEmptyArrayStorage(dict);
        Assert.NotNull(dict._propertyStorage);
        Assert.Collection(
            dict,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp));
    }

    [Fact]
    public void TryAdd_EmptyStorage_CanAdd()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.TryAdd("key", "value");

        // Assert
        Assert.True(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key", "value"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_ArrayStorage_CanAdd()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key0", "value0" },
            };

        // Act
        var result = dict.TryAdd("key1", "value1");

        // Assert
        Assert.True(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key0", "value0"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key1", "value1"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_ArrayStorage_CanAddWithResize()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key0", "value0" },
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
            };

        // Act
        var result = dict.TryAdd("key4", "value4");

        // Assert
        Assert.True(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key0", "value0"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key1", "value1"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key2", "value2"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key3", "value3"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key4", "value4"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryAdd_ArrayStorage_DoesNotAddWhenKeyIsPresent()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key0", "value0" },
            };

        // Act
        var result = dict.TryAdd("key0", "value1");

        // Assert
        Assert.False(result);
        Assert.Collection(
            dict._arrayStorage,
            kvp => Assert.Equal(new KeyValuePair<string, object?>("key0", "value0"), kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp),
            kvp => Assert.Equal(default, kvp));
    }

    [Fact]
    public void TryGetValue_EmptyStorage()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.TryGetValue("key", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_EmptyStringIsAllowed()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act
        var result = dict.TryGetValue("", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_PropertyStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.TryGetValue("other", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void TryGetValue_PropertyStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.TryGetValue("key", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void TryGetValue_PropertyStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary(new { key = "value" });

        // Act
        var result = dict.TryGetValue("kEy", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.NotNull(dict._propertyStorage);
    }

    [Fact]
    public void TryGetValue_ListStorage_False()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("other", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryGetValue_ListStorage_True()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("key", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void TryGetValue_ListStorage_True_CaseInsensitive()
    {
        // Arrange
        var dict = new RouteValueDictionary()
            {
                { "key", "value" },
            };

        // Act
        var result = dict.TryGetValue("kEy", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("value", value);
        Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
    }

    [Fact]
    public void ListStorage_DynamicallyAdjustsCapacity()
    {
        // Arrange
        var dict = new RouteValueDictionary();

        // Act 1
        dict.Add("key", "value");

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(4, storage.Length);

        // Act 2
        dict.Add("key2", "value2");
        dict.Add("key3", "value3");
        dict.Add("key4", "value4");
        dict.Add("key5", "value5");

        // Assert 2
        storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(8, storage.Length);
    }

    [Fact]
    public void ListStorage_RemoveAt_RearrangesInnerArray()
    {
        // Arrange
        var dict = new RouteValueDictionary();
        dict.Add("key", "value");
        dict.Add("key2", "value2");
        dict.Add("key3", "value3");

        // Assert 1
        var storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(3, dict.Count);

        // Act
        dict.Remove("key2");

        // Assert 2
        storage = Assert.IsType<KeyValuePair<string, object?>[]>(dict._arrayStorage);
        Assert.Equal(2, dict.Count);
        Assert.Equal("key", storage[0].Key);
        Assert.Equal("value", storage[0].Value);
        Assert.Equal("key3", storage[1].Key);
        Assert.Equal("value3", storage[1].Value);
    }

    [Fact]
    public void FromArray_TakesOwnershipOfArray()
    {
        // Arrange
        var array = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>("a", 0),
                new KeyValuePair<string, object?>("b", 1),
                new KeyValuePair<string, object?>("c", 2),
        };

        var dictionary = RouteValueDictionary.FromArray(array);

        // Act - modifying the array should modify the dictionary
        array[0] = new KeyValuePair<string, object?>("aa", 10);

        // Assert
        Assert.Equal(3, dictionary.Count);
        Assert.Equal(10, dictionary["aa"]);
    }

    [Fact]
    public void FromArray_EmptyArray()
    {
        // Arrange
        var array = Array.Empty<KeyValuePair<string, object?>>();

        // Act
        var dictionary = RouteValueDictionary.FromArray(array);

        // Assert
        Assert.Empty(dictionary);
    }

    [Fact]
    public void FromArray_RemovesGapsInArray()
    {
        // Arrange
        var array = new KeyValuePair<string, object?>[]
        {
                new KeyValuePair<string, object?>(null!, null),
                new KeyValuePair<string, object?>("a", 0),
                new KeyValuePair<string, object?>(null!, null),
                new KeyValuePair<string, object?>(null!, null),
                new KeyValuePair<string, object?>("b", 1),
                new KeyValuePair<string, object?>("c", 2),
                new KeyValuePair<string, object?>("d", 3),
                new KeyValuePair<string, object?>(null!, null),
        };

        // Act - calling From should modify the array
        var dictionary = RouteValueDictionary.FromArray(array);

        // Assert
        Assert.Equal(4, dictionary.Count);
        Assert.Equal(
            new KeyValuePair<string, object?>[]
            {
                    new KeyValuePair<string, object?>("d", 3),
                    new KeyValuePair<string, object?>("a", 0),
                    new KeyValuePair<string, object?>("c", 2),
                    new KeyValuePair<string, object?>("b", 1),
                    new KeyValuePair<string, object?>(null!, null),
                    new KeyValuePair<string, object?>(null!, null),
                    new KeyValuePair<string, object?>(null!, null),
                    new KeyValuePair<string, object?>(null!, null),
            },
            array);
    }

    private void AssertEmptyArrayStorage(RouteValueDictionary value)
    {
        Assert.Same(Array.Empty<KeyValuePair<string, object?>>(), value._arrayStorage);
    }

    private class RegularType
    {
        public bool IsAwesome { get; set; }

        public int CoolnessFactor { get; set; }
    }

    private class Visibility
    {
        private string? PrivateYo { get; set; }

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
        public string? City { get; set; }

        public string? State { get; set; }
    }

    private struct StructAddress
    {
        public string? City { get; set; }

        public string? State { get; set; }
    }
}
