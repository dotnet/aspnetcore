// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

public class FormDataMapperTests
{
    [Theory]
    [MemberData(nameof(PrimitiveTypesData))]
    public void CanDeserialize_PrimitiveTypes(string value, Type type, object expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Red", Colors.Red)]
    [InlineData("RED", Colors.Red)]
    [InlineData("BlUe", Colors.Blue)]
    [InlineData("green", Colors.Green)]
    public void CanDeserialize_EnumTypes(string value, Colors expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(Colors));

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Red", Colors.Red)]
    [InlineData("RED", Colors.Red)]
    [InlineData("BlUe", Colors.Blue)]
    [InlineData("green", Colors.Green)]
    public void CanDeserialize_NullableEnumTypes(string value, Colors expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(Colors?));

        // Assert
        Assert.Equal(expected, result);
    }

    private FormDataReader CreateFormDataReader(Dictionary<string, StringValues> collection, CultureInfo invariantCulture, IFormFileCollection formFileCollection = null)
    {
        var dictionary = new Dictionary<FormKey, StringValues>(collection.Count);
        foreach (var kvp in collection)
        {
            dictionary.Add(new FormKey(kvp.Key.AsMemory()), kvp.Value);
        }
        return formFileCollection is null
            ? new FormDataReader(dictionary, CultureInfo.InvariantCulture, new char[2048])
            : new FormDataReader(dictionary, CultureInfo.InvariantCulture, new char[2048], formFileCollection);
    }

    [Theory]
    [MemberData(nameof(PrimitiveTypesData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
    public void PrimitiveTypes_MissingValues_DoNotAddErrors(string _, Type type, object __)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Equal(type.IsValueType ? Activator.CreateInstance(type) : null, result);
        Assert.Empty(errors);
    }

    [Fact]
    public void Throws_ForInvalidValues()
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues("abc") };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act & Assert
        var exception = Assert.Throws<FormDataMappingException>(() => FormDataMapper.Map<int>(reader, options));
        Assert.NotNull(exception?.Error);
        Assert.Equal("value", exception.Error.Key);
        Assert.Equal("The value 'abc' is not valid for 'value'.", exception.Error.Message.ToString(reader.Culture));
        Assert.Equal("abc", exception.Error.Value);
    }

    [Fact]
    public void CanCollectErrors_WithCustomHandler_ForInvalidValues()
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues("abc") };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act & Assert
        var result = FormDataMapper.Map<int>(reader, options);
        Assert.Equal(default, result);
        var error = Assert.Single(errors);
        Assert.NotNull(error);
        Assert.Equal("value", error.Key);
        Assert.Equal("The value 'abc' is not valid for 'value'.", error.Message.ToString(reader.Culture));
        Assert.Equal("abc", error.Value);
    }

    [Theory]
    [MemberData(nameof(NullableBasicTypes))]
    public void CanDeserialize_NullablePrimitiveTypes(string value, Type type, object expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(NullNullableBasicTypes))]
    public void CanDeserialize_NullValues(Type type)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(UriTestData))]
    public void CanDeserialize_Uri(string value, Uri expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Uri>(reader, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(UriTestData))]
    public void CanDeserialize_ComplexTypes_WithUriProperties(string value, Uri expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value.Slug"] = new StringValues(value) };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<TypeWithUri>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result.Slug);
    }

    public static TheoryData<string, Uri> UriTestData => new TheoryData<string, Uri>
    {
        { "http://www.example.com", new Uri("http://www.example.com") },
        { "http://www.example.com/path", new Uri("http://www.example.com/path") },
        { "http://www.example.com/path/", new Uri("http://www.example.com/path/") },
        { "/path", new Uri("/path", UriKind.Relative) },
    };

    [Fact]
    public void CanDeserialize_CustomParsableTypes()
    {
        // Arrange
        var expected = new Point { X = 1, Y = 1 };
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues("(1,1)") };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Point>(reader, options);

        // Assert
        Assert.Equal(expected, result);
    }

#nullable enable
    [Fact]
    public void CanDeserialize_NullableCustomParsableTypes()
    {
        // Arrange
        var expected = new ValuePoint { X = 1, Y = 1 };
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues("(1,1)") };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ValuePoint?>(reader, options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_NullableCustomParsableTypes_NullValue()
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ValuePoint?>(reader, options);

        // Assert
        Assert.Null(result);
    }
#nullable disable

    [Fact]
    public void Deserialize_Collections_NoElements_ReturnsNull()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_Collections_SingleElement_ReturnsCollection()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["[0]"] = "10" };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        var value = Assert.Single(result);
        Assert.Equal(10, value);
    }

    [Fact]
    public void Deserialize_Collections_SupportsMultipleElementsPerKey_ForSingleValueElementTypes_ParsableTypes()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["values"] = new StringValues(new[] { "10", "11" }) };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("values");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Collection(result,
            v => Assert.Equal(10, v),
            v => Assert.Equal(11, v));
    }

    [Fact]
    public void Deserialize_Collections_SupportsMultipleElementsPerKey_ForSingleValueElementTypes_EnumTypes()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["values"] = new StringValues(new[] { "Red", "Blue" }) };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("values");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<Colors>>(reader, options);

        // Assert
        Assert.Collection(result,
            v => Assert.Equal(Colors.Red, v),
            v => Assert.Equal(Colors.Blue, v));
    }

    [Fact]
    public void Deserialize_Collections_SupportsMultipleElementsPerKey_ForSingleValueElementTypes_Nullable_WhenUnderlyingElementIsSingleValue()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["values"] = new StringValues(new[] { "Red", "Blue" }) };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("values");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<Colors?>>(reader, options);

        // Assert
        Assert.Collection(result,
            v => Assert.Equal(Colors.Red, v),
            v => Assert.Equal(Colors.Blue, v));
    }

    [Fact]
    public void Deserialize_Collections_MultipleElementsPerKey_CanReportErrors()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["values"] = new StringValues(new[] { "10", "a" }) };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("values");
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Equal(10, Assert.Single(result));
        var error = Assert.Single(errors);
        Assert.Equal("values", error.Key);
        Assert.Equal("The value 'a' is not valid for 'values'.", error.Message.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Deserialize_Collections_MultipleElementsPerKey_ContinuesProcessingValuesAfterErrors()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>() { ["values"] = new StringValues(new[] { "10", "a", "11" }) };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("values");
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Collection(result,
            v => Assert.Equal(10, v),
            v => Assert.Equal(11, v));
        var error = Assert.Single(errors);
        Assert.Equal("values", error.Key);
        Assert.Equal("The value 'a' is not valid for 'values'.", error.Message.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(101)]
    public void Deserialize_Collections_HandlesComputedIndexesBoundaryCorrectly(int size)
    {
        // Arrange
        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = 110
        };

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Equal(size, result.Count);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(109)]
    [InlineData(110)]
    [InlineData(120)]
    public void Deserialize_Collections_PoolsArraysCorrectly(int size)
    {
        // Arrange
        var rented = new List<int[]>();
        var returned = new List<int[]>();

        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = 140
        };

        var converter = new CollectionConverter<
                int[],
                TestArrayPoolBufferAdapter,
                TestArrayPoolBufferAdapter.PooledBuffer,
                int>(new ParsableConverter<int>());

        options.AddConverter(converter);

        TestArrayPoolBufferAdapter.OnRent += rented.Add;
        TestArrayPoolBufferAdapter.OnReturn += returned.Add;

        // Act
        var result = FormDataMapper.Map<int[]>(reader, options);

        TestArrayPoolBufferAdapter.OnRent -= rented.Add;
        TestArrayPoolBufferAdapter.OnReturn -= returned.Add;

        // Assert
        Assert.Equal(rented.Count, returned.Count);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(101)]
    [InlineData(109)]
    [InlineData(110)]
    [InlineData(120)]
    public void Deserialize_Collections_AlwaysReturnsBuffer(int size)
    {
        // Arrange
        var rented = new List<int[]>();
        var returned = new List<int[]>();

        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = 140
        };

        var elementConverter = new ThrowingConverter();
        elementConverter.OnTryReadDelegate = (ref FormDataReader context, Type type, FormDataMapperOptions options, out int result, out bool found) =>
        {
            context.TryGetValue(out var value);
            var index = int.Parse(value, CultureInfo.InvariantCulture) - 10;
            if (index + 1 == size)
            {
                throw new InvalidOperationException("Can't parse this!");
            }
            result = default;
            found = true;
            return false;
        };

        var converter = new CollectionConverter<
                int[],
                TestArrayPoolBufferAdapter,
                TestArrayPoolBufferAdapter.PooledBuffer,
                int>(elementConverter);

        options.AddConverter(converter);

        TestArrayPoolBufferAdapter.OnRent += rented.Add;
        TestArrayPoolBufferAdapter.OnReturn += returned.Add;

        // Act
        var result = Assert.Throws<InvalidOperationException>(() => FormDataMapper.Map<int[]>(reader, options));

        TestArrayPoolBufferAdapter.OnRent -= rented.Add;
        TestArrayPoolBufferAdapter.OnReturn -= returned.Add;

        // Assert
        Assert.Equal(rented.Count, returned.Count);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(49, 50)]
    [InlineData(50, 50)]
    [InlineData(51, 50)]
    [InlineData(60, 50)]
    [InlineData(109, 110)]
    [InlineData(110, 110)]
    [InlineData(111, 110)]
    [InlineData(120, 110)]
    public void Deserialize_Collections_RespectsMaxCollectionSize(int size, int maxCollectionSize)
    {
        // Arrange
        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = maxCollectionSize
        };

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.True(result.Count == maxCollectionSize || result.Count == maxCollectionSize - 1);
        if (size > maxCollectionSize)
        {
            var error = Assert.Single(errors);
            Assert.Equal("", error.Key);
            Assert.Equal($"The number of elements in the collection exceeded the maximum number of '{maxCollectionSize}' elements allowed.", error.Message.ToString(reader.Culture));
            Assert.Null(error.Value);
        }
        else
        {
            Assert.Empty(errors);
        }
    }

    [Fact]
    public void Deserialize_Collections_ContinuesParsingAfterErrors()
    {
        // Arrange
        var expected = new List<int> { 0, 11, 12, 13, 0, 15, 16, 17, 18, 19 };
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "abc",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "def",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };

        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        var list = Assert.IsType<List<int>>(result);
        Assert.Equal(expected, list);
        Assert.Equal(2, errors.Count);
        Assert.Collection(errors,
            e =>
            {
                Assert.Equal("[0]", e.Key);
                Assert.Equal("The value 'abc' is not valid for '0'.", e.Message.ToString(reader.Culture));
                Assert.Equal("abc", e.Value);
            },
            e =>
            {
                Assert.Equal("[4]", e.Key);
                Assert.Equal("The value 'def' is not valid for '4'.", e.Message.ToString(reader.Culture));
                Assert.Equal("def", e.Value);
            });
    }

    [Fact]
    public void CanDeserialize_Collections_IReadOnlySet()
    {
        // Arrange
        var expected = new HashSet<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<IReadOnlySet<int>, HashSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IReadOnlyListOfT()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<IReadOnlyList<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IReadOnlyCollection()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<IReadOnlyCollection<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ISet()
    {
        // Arrange
        var expected = new HashSet<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<ISet<int>, HashSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IListOfT()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<IList<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ICollectionOfT()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<ICollection<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IEnumerableOfT()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<IEnumerable<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ArrayOfT()
    {
        // Arrange
        var expected = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<int[], int[], int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_SortedSet()
    {
        // Arrange
        var expected = new SortedSet<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<SortedSet<int>, SortedSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_HashSet()
    {
        // Arrange
        var expected = new HashSet<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<HashSet<int>, HashSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ReadOnlyCollectionOfT()
    {
        // Arrange
        var expected = new ReadOnlyCollection<int>(new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ReadOnlyCollection<int>, ReadOnlyCollection<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_CollectionOfT()
    {
        // Arrange
        var expected = new Collection<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<Collection<int>, Collection<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ListOfT()
    {
        // Arrange
        var expected = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<List<int>, List<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_LinkedList()
    {
        // Arrange
        var expected = new LinkedList<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<LinkedList<int>, LinkedList<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_Queue()
    {
        // Arrange
        var expected = new Queue<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<Queue<int>, Queue<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_Stack()
    {
        // Arrange
        var expected = new Stack<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<Stack<int>, Stack<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ConcurrentBag()
    {
        // Arrange
        var expected = new ConcurrentBag<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ConcurrentBag<int>, ConcurrentBag<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ConcurrentQueue()
    {
        // Arrange
        var expected = new ConcurrentQueue<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ConcurrentQueue<int>, ConcurrentQueue<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ConcurrentStack()
    {
        // Arrange
        var expected = new ConcurrentStack<int>(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ConcurrentStack<int>, ConcurrentStack<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableArray()
    {
        // Arrange
        var expected = ImmutableArray.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableArray<int>, ImmutableArray<int>, int>(expected, sequenceEquals: true);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableList()
    {
        // Arrange
        var expected = ImmutableList.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableList<int>, ImmutableList<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableHashSet()
    {
        // Arrange
        var expected = ImmutableHashSet.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableHashSet<int>, ImmutableHashSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableSortedSet()
    {
        // Arrange
        var expected = ImmutableSortedSet.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableSortedSet<int>, ImmutableSortedSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableQueue()
    {
        // Arrange
        var expected = ImmutableQueue.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableQueue<int>, ImmutableQueue<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_ImmutableStack()
    {
        // Arrange
        var expected = ImmutableStack.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<ImmutableStack<int>, ImmutableStack<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IImmutableList()
    {
        // Arrange
        var expected = ImmutableList.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<IImmutableList<int>, ImmutableList<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IImmutableSet()
    {
        // Arrange
        var expected = ImmutableHashSet.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<IImmutableSet<int>, ImmutableHashSet<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IImmutableQueue()
    {
        // Arrange
        var expected = ImmutableQueue.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<IImmutableQueue<int>, ImmutableQueue<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_IImmutableStack()
    {
        // Arrange
        var expected = ImmutableStack.CreateRange(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
        CanDeserialize_Collection<IImmutableStack<int>, ImmutableStack<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Collections_CustomCollection()
    {
        // Arrange
        var expected = new CustomCollection<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        CanDeserialize_Collection<CustomCollection<int>, CustomCollection<int>, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_Dictionary()
    {
        // Arrange
        var expected = new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        CanDeserialize_Dictionary<Dictionary<int, int>, Dictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_ConcurrentDictionary()
    {
        // Arrange
        var expected = new ConcurrentDictionary<int, int>(new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, });
        CanDeserialize_Dictionary<ConcurrentDictionary<int, int>, ConcurrentDictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_ImmutableDictionary()
    {
        // Arrange
        var expected = ImmutableDictionary.CreateRange(new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, });
        CanDeserialize_Dictionary<ImmutableDictionary<int, int>, ImmutableDictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_ImmutableSortedDictionary()
    {
        // Arrange
        var expected = ImmutableSortedDictionary.CreateRange(new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, });
        CanDeserialize_Dictionary<ImmutableSortedDictionary<int, int>, ImmutableSortedDictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_IImmutableDictionary()
    {
        // Arrange
        var expected = ImmutableDictionary.CreateRange(new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, });
        // Arrange
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<IImmutableDictionary<int, int>>(reader, options);

        // Assert
        var dictionary = Assert.IsType<ImmutableDictionary<int, int>>(result);
        Assert.Equal(expected.Count, dictionary.Count);
        Assert.Equal(expected.OrderBy(o => o.Key).ToArray(), dictionary.OrderBy(o => o.Key).ToArray());
    }

    [Fact]
    public void CanDeserialize_Dictionary_IDictionary()
    {
        // Arrange
        var expected = new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        CanDeserialize_Dictionary<IDictionary<int, int>, Dictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_SortedList()
    {
        // Arrange
        var expected = new SortedList<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        CanDeserialize_Dictionary<SortedList<int, int>, SortedList<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_SortedDictionary()
    {
        // Arrange
        var expected = new SortedDictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        CanDeserialize_Dictionary<SortedDictionary<int, int>, SortedDictionary<int, int>, int, int>(expected);
    }

    [Fact]
    public void CanDeserialize_Dictionary_IReadOnlyDictionary()
    {
        // Arrange
        var expected = new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<IReadOnlyDictionary<int, int>>(reader, options);

        // Assert
        var dictionary = Assert.IsType<ReadOnlyDictionary<int, int>>(result);
        Assert.Equal(expected.Count, dictionary.Count);
        Assert.Equal(expected.OrderBy(o => o.Key).ToArray(), dictionary.OrderBy(o => o.Key).ToArray());
    }

    [Fact]
    public void CanDeserialize_Dictionary_ReadOnlyDictionary()
    {
        // Arrange
        var expected = new Dictionary<int, int>() { [0] = 10, [1] = 11, [2] = 12, [3] = 13, [4] = 14, [5] = 15, [6] = 16, [7] = 17, [8] = 18, [9] = 19, };
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ReadOnlyDictionary<int, int>>(reader, options);

        // Assert
        var dictionary = Assert.IsType<ReadOnlyDictionary<int, int>>(result);
        Assert.Equal(expected.Count, dictionary.Count);
        Assert.Equal(expected.OrderBy(o => o.Key).ToArray(), dictionary.OrderBy(o => o.Key).ToArray());
    }

    [Fact]
    public void Deserialize_EmptyDictionary_ReturnsNull()
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<IReadOnlyDictionary<int, int>>(reader, options);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(109, 110)]
    [InlineData(110, 110)]
    [InlineData(111, 110)]
    [InlineData(120, 110)]
    public void Deserialize_Dictionary_RespectsMaxCollectionSize(int size, int maxCollectionSize)
    {
        // Arrange
        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = maxCollectionSize
        };

        // Act
        var result = FormDataMapper.Map<Dictionary<int, int>>(reader, options);

        // Assert
        Assert.True(result.Count == maxCollectionSize || result.Count == maxCollectionSize - 1);
        if (size > maxCollectionSize)
        {
            var error = Assert.Single(errors);
            Assert.Equal("", error.Key);
            Assert.Equal($"The number of elements in the dictionary exceeded the maximum number of '{maxCollectionSize}' elements allowed.", error.Message.ToString(reader.Culture));
            Assert.Null(error.Value);
        }
        else
        {
            Assert.Empty(errors);
        }
    }

    [Fact]
    public void Deserialize_Dictionary_ContinuesParsingAfterErrors()
    {
        // Arrange
        var expected = new Dictionary<int, int>
        {
            [0] = 0,
            [1] = 11,
            [2] = 12,
            [3] = 13,
            [4] = 0,
            [5] = 15,
            [6] = 16,
            [7] = 17,
            [8] = 18,
            [9] = 19,
        };
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "abc",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "def",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };

        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Dictionary<int, int>>(reader, options);

        // Assert
        var dictionary = Assert.IsType<Dictionary<int, int>>(result);
        Assert.Equal(expected, dictionary);
        Assert.Equal(2, errors.Count);
        Assert.Collection(errors,
            e =>
            {
                Assert.Equal("[0]", e.Key);
                Assert.Equal("The value 'abc' is not valid for '0'.", e.Message.ToString(reader.Culture));
                Assert.Equal("abc", e.Value);
            },
            e =>
            {
                Assert.Equal("[4]", e.Key);
                Assert.Equal("The value 'def' is not valid for '4'.", e.Message.ToString(reader.Culture));
                Assert.Equal("def", e.Value);
            });
    }

    [Fact]
    public void Deserialize_SkipsElement_WhenFailsToParseKey()
    {
        // Arrange
        var expected = new Dictionary<int, int>
        {
            [1] = 11,
            [2] = 12,
            [3] = 13,
            [5] = 15,
            [6] = 16,
            [7] = 17,
            [8] = 18,
            [9] = 19,
        };
        var collection = new Dictionary<string, StringValues>()
        {
            ["[abc]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[def]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };

        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Dictionary<int, int>>(reader, options);

        // Assert
        var dictionary = Assert.IsType<Dictionary<int, int>>(result);
        Assert.Equal(expected, dictionary);
        Assert.Equal(2, errors.Count);
        Assert.Collection(errors,
            e =>
            {
                Assert.Equal("", e.Key);
                Assert.Equal("The value 'abc' is not a valid key for ''.", e.Message.ToString(reader.Culture));
                Assert.Null(e.Value);
            },
            e =>
            {
                Assert.Equal("", e.Key);
                Assert.Equal("The value 'def' is not a valid key for ''.", e.Message.ToString(reader.Culture));
                Assert.Null(e.Value);
            });
    }

    private void CanDeserialize_Dictionary<TDictionary, TImplementation, TKey, TValue>(TImplementation expected)
        where TDictionary : IDictionary<TKey, TValue>
        where TImplementation : TDictionary
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(TDictionary));

        // Assert
        var dictionary = Assert.IsType<TImplementation>(result);
        Assert.Equal(expected.Count, dictionary.Count);
        Assert.Equal(expected.OrderBy(o => o.Key).ToArray(), dictionary.OrderBy(o => o.Key).ToArray());
    }

    private void CanDeserialize_Collection<TCollection, TImplementation, TElement>(TImplementation expected, bool sequenceEquals = false)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>()
        {
            ["[0]"] = "10",
            ["[1]"] = "11",
            ["[2]"] = "12",
            ["[3]"] = "13",
            ["[4]"] = "14",
            ["[5]"] = "15",
            ["[6]"] = "16",
            ["[7]"] = "17",
            ["[8]"] = "18",
            ["[9]"] = "19",
        };
        var reader = CreateFormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(TCollection));

        // Assert
        var list = Assert.IsType<TImplementation>(result);
        if (!sequenceEquals)
        {
            Assert.Equal(expected, list);
        }
        else
        {
            Assert.True(((IEnumerable<TElement>)expected).SequenceEqual((IEnumerable<TElement>)list));
        }
    }

    [Fact]
    public void CanDeserialize_ComplexValueType_Address()
    {
        // Arrange
        var expected = new Address() { City = "Redmond", Street = "1 Microsoft Way", Country = "United States", ZipCode = 98052 };
        var data = new Dictionary<string, StringValues>()
        {
            ["City"] = "Redmond",
            ["Country"] = "United States",
            ["Street"] = "1 Microsoft Way",
            ["ZipCode"] = "98052",
        };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Address>(reader, options);

        // Assert
        Assert.Equal(expected.City, result.City);
        Assert.Equal(expected.Street, result.Street);
        Assert.Equal(expected.Country, result.Country);
        Assert.Equal(expected.ZipCode, result.ZipCode);
    }

    [Fact]
    public void CanDeserialize_ComplexReferenceType_Customer()
    {
        // Arrange
        var expected = new Customer() { Age = 20, Name = "John Doe", Email = "john.doe@example.com", IsPreferred = true };
        var data = new Dictionary<string, StringValues>()
        {
            ["Age"] = "20",
            ["Name"] = "John Doe",
            ["Email"] = "john.doe@example.com",
            ["IsPreferred"] = "true",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Customer>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Age, result.Age);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Email, result.Email);
        Assert.Equal(expected.IsPreferred, result.IsPreferred);
    }

    [Fact]
    public void CanDeserialize_ComplexRecursiveTypes_RecursiveList()
    {
        // Arrange
        var expected = new RecursiveList()
        {
            Head = 10,
            Tail = null
        };

        for (var i = 10 - 1; i >= 0; i--)
        {
            expected = new RecursiveList()
            {
                Head = i,
                Tail = expected
            };
        }

        var data = new Dictionary<string, StringValues>()
        {
            ["Head"] = "0",
            ["Tail.Head"] = "1",
            ["Tail.Tail.Head"] = "2",
            ["Tail.Tail.Tail.Head"] = "3",
            ["Tail.Tail.Tail.Tail.Head"] = "4",
            ["Tail.Tail.Tail.Tail.Tail.Head"] = "5",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "6",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "7",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "8",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "9",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "10",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<RecursiveList>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(expected.Head, result.Head);
            Assert.Equal(expected.Tail.Head, result.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Head, result.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head);
            Assert.Null(result.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail);
        });
    }

    [Fact]
    public void CanDeserialize_ComplexRecursiveTypes_ThrowsWhenMaxRecursionDepthExceeded()
    {
        // Arrange
        var expected = new RecursiveList()
        {
            Head = 5,
            Tail = null
        };

        for (var i = 5 - 1; i >= 0; i--)
        {
            expected = new RecursiveList()
            {
                Head = i,
                Tail = expected
            };
        }

        var data = new Dictionary<string, StringValues>()
        {
            ["Head"] = "0",
            ["Tail.Head"] = "1",
            ["Tail.Tail.Head"] = "2",
            ["Tail.Tail.Tail.Head"] = "3",
            ["Tail.Tail.Tail.Tail.Head"] = "4",
            ["Tail.Tail.Tail.Tail.Tail.Head"] = "5",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "6",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "7",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "8",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "9",
            ["Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Tail.Head"] = "10",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.MaxRecursionDepth = 5;
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, exception) =>
        {
            errors.Add(new FormDataMappingError(key, message, exception));
        };

        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<RecursiveList>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(expected.Head, result.Head);
            Assert.Equal(expected.Tail.Head, result.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Head, result.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Head);
            Assert.Equal(expected.Tail.Tail.Tail.Tail.Head, result.Tail.Tail.Tail.Tail.Head);
            Assert.Null(result.Tail.Tail.Tail.Tail.Tail);
        });
        Assert.Collection(errors,
            e =>
            {
                Assert.Equal("Tail.Tail.Tail.Tail.Tail", e.Key);
                Assert.Equal("The maximum recursion depth of '5' was exceeded for 'Tail.Tail.Tail.Tail.Tail.Head'.", e.Message.ToString(CultureInfo.InvariantCulture));
            },
            e =>
            {
                Assert.Equal("Tail.Tail.Tail.Tail.Tail", e.Key);
                Assert.Equal("The maximum recursion depth of '5' was exceeded for 'Tail.Tail.Tail.Tail.Tail.Tail'.", e.Message.ToString(CultureInfo.InvariantCulture));
            });
    }

    [Fact]
    public void CanDeserialize_ComplexRecursiveCollectionTypes_RecursiveTree()
    {
        // Arrange
        var expected = new RecursiveTree()
        {
            Value = 10,
            Children = null
        };

        for (var i = 10 - 1; i >= 0; i--)
        {
            expected = new RecursiveTree()
            {
                Value = i,
                Children = new List<RecursiveTree>() { expected }
            };
        }

        var data = new Dictionary<string, StringValues>()
        {
            ["Value"] = "0",
            ["Children[0].Value"] = "1",
            ["Children[0].Children[0].Value"] = "2",
            ["Children[0].Children[0].Children[0].Value"] = "3",
            ["Children[0].Children[0].Children[0].Children[0].Value"] = "4",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "5",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "6",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "7",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "8",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "9",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "10",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<RecursiveTree>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(expected.Value, result.Value);
            Assert.Equal(expected.Children[0].Value, result.Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Value, result.Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Null(result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children);
        });
    }

    [Fact]
    public void CanDeserialize_ComplexRecursiveCollectionTypes_RecursiveDictionaryTree()
    {
        // Arrange
        var expected = new RecursiveDictionaryTree()
        {
            Value = 10,
            Children = null
        };

        for (var i = 10 - 1; i >= 0; i--)
        {
            expected = new RecursiveDictionaryTree()
            {
                Value = i,
                Children = new Dictionary<int, RecursiveDictionaryTree>() { [0] = expected }
            };
        }

        var data = new Dictionary<string, StringValues>()
        {
            ["Value"] = "0",
            ["Children[0].Value"] = "1",
            ["Children[0].Children[0].Value"] = "2",
            ["Children[0].Children[0].Children[0].Value"] = "3",
            ["Children[0].Children[0].Children[0].Children[0].Value"] = "4",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "5",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "6",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "7",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "8",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "9",
            ["Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value"] = "10",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<RecursiveTree>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Multiple(() =>
        {
            Assert.Equal(expected.Value, result.Value);
            Assert.Equal(expected.Children[0].Value, result.Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Value, result.Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Equal(expected.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value, result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Value);
            Assert.Null(result.Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children[0].Children);
        });
    }

    [Fact]
    public void Deserialize_ComplexType_ContinuesMappingAfterPropertyError()
    {
        // Arrange
        var expected = new Customer() { Age = 0, Name = "John Doe", Email = "john.doe@example.com", IsPreferred = true };
        var data = new Dictionary<string, StringValues>()
        {
            ["Age"] = "abc",
            ["Name"] = "John Doe",
            ["Email"] = "john.doe@example.com",
            ["IsPreferred"] = "true",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, exception) =>
        {
            errors.Add(new FormDataMappingError(key, message, exception));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Customer>(reader, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Age, result.Age);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Email, result.Email);
        Assert.Equal(expected.IsPreferred, result.IsPreferred);

        var error = Assert.Single(errors);
        Assert.Equal("Age", error.Key);
        var expectedMessage = "The value 'abc' is not valid for 'Age'.";
        var actualMessage = error.Message.ToString(reader.Culture);
        Assert.Equal(expectedMessage, actualMessage);
        Assert.Equal("abc", error.Value);
    }

    [Fact]
    public void CanDeserialize_ComplexReferenceType_Inheritance()
    {
        // Arrange
        var expected = new FrequentCustomer() { Age = 20, Name = "John Doe", Email = "john@example.com", IsPreferred = true, TotalVisits = 10, PreferredStore = "Redmond", MonthlyFrequency = 0.8 };
        var data = new Dictionary<string, StringValues>()
        {
            ["Age"] = "20",
            ["Name"] = "John Doe",
            ["Email"] = "john@example.com",
            ["IsPreferred"] = "true",
            ["TotalVisits"] = "10",
            ["PreferredStore"] = "Redmond",
            ["MonthlyFrequency"] = "0.8",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<FrequentCustomer>(reader, options);
        Assert.NotNull(result);
        Assert.Equal(expected.Age, result.Age);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Email, result.Email);
        Assert.Equal(expected.IsPreferred, result.IsPreferred);
        Assert.Equal(expected.TotalVisits, result.TotalVisits);
        Assert.Equal(expected.PreferredStore, result.PreferredStore);
        Assert.Equal(expected.MonthlyFrequency, result.MonthlyFrequency);
    }

    [Fact]
    public void CanDeserialize_ComplexTypeWithConstructorParameters_KeyValuePair()
    {
        // Arrange
        var expected = new KeyValuePair<string, int>("Age", 20);
        var data = new Dictionary<string, StringValues>()
        {
            ["Key"] = "Age",
            ["Value"] = "20",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<KeyValuePair<string, int>>(reader, options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_RecordType()
    {
        // Arrange
        var expected = new ClassRecordType("Age", 20);
        var data = new Dictionary<string, StringValues>()
        {
            ["Key"] = "Age",
            ["Value"] = "20",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ClassRecordType>(reader, options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_StructRecordType()
    {
        // Arrange
        var expected = new StructRecordType("Age", 20);
        var data = new Dictionary<string, StringValues>()
        {
            ["Key"] = "Age",
            ["Value"] = "20",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<StructRecordType>(reader, options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_AppliesDataMemberRelatedAttributes()
    {
        // Arrange
        var expected = new DataMemberAttributesType { Key = "Age", Value = 20 };
        var data = new Dictionary<string, StringValues>()
        {
            ["mycustomkey"] = "Age",
            ["mycustomvalue"] = "20",
            ["Ignored"] = "This should be ignored",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<DataMemberAttributesType>(reader, options);
        Assert.Equal(expected.Key, result.Key);
        Assert.Equal(expected.Value, result.Value);
        Assert.Null(result.Ignored);
    }

    [Fact]
    public void CanDeserialize_ComplexType_AppliesDataMemberRelatedAttributes_FromMatchingConstructorParameters()
    {
        // Arrange
        var expected = new DataMemberAttributesConstructorType("Age", 20);
        var data = new Dictionary<string, StringValues>()
        {
            ["mycustomkey"] = "Age",
            ["mycustomvalue"] = "20",
            ["Ignored"] = "This should be ignored",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<DataMemberAttributesConstructorType>(reader, options);
        Assert.Equal(expected.Key, result.Key);
        Assert.Equal(expected.Value, result.Value);
        Assert.Null(result.Ignored);
    }

    [Fact]
    public void CanDeserialize_ComplexType_IgnoresPropertiesWithoutPublicSetters()
    {
        // Arrange
        var expected = new TypeIgnoresReadOnlyProperties() { Name = "John" };
        var data = new Dictionary<string, StringValues>()
        {
            ["Id"] = "1",
            ["Name"] = "John",
            ["Age"] = "20",
            ["Email"] = "john@doe.com",
            ["IsPreferred"] = "true",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<TypeIgnoresReadOnlyProperties>(reader, options);
        Assert.Equal(0, result.Id);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
        Assert.Equal(expected.Email, result.Email);
        Assert.Equal(expected.IsPreferred, result.IsPreferred);
    }

    [Fact]
    public void CanDeserialize_ComplexType_RequiredProperties()
    {
        // Arrange
        var expected = new TypeRequiredProperties() { Name = null, Age = 20 };
        var data = new Dictionary<string, StringValues>()
        {
            ["Age"] = "20",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<TypeRequiredProperties>(reader, options);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Age, result.Age);
        Assert.Single(errors);
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanDeserializeTuples()
    {
        // Arrange
        var expected = new Tuple<int, string>(1, "John");
        var data = new Dictionary<string, StringValues>()
        {
            ["Item1"] = "1",
            ["Item2"] = "John",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<Tuple<int, string>>(reader, options);
        Assert.Equal(expected.Item1, result.Item1);
        Assert.Equal(expected.Item2, result.Item2);
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanDeserializeValueTuples()
    {
        // Arrange
        var expected = new ValueTuple<int, string>(1, "John");
        var data = new Dictionary<string, StringValues>()
        {
            ["Item1"] = "1",
            ["Item2"] = "John",
        };

        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ValueTuple<int, string>>(reader, options);
        Assert.Equal(expected.Item1, result.Item1);
        Assert.Equal(expected.Item2, result.Item2);
    }

    [Fact]
    public void CanDeserialize_ComplexType_DoesNotRegisterMissingRequiredParametersIfNoValueFound()
    {
        // Arrange
        var expected = new ThrowsWithMissingParameterValue("Age");
        var data = new Dictionary<string, StringValues>() { };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ThrowsWithMissingParameterValue>(reader, options);

        // Assert
        Assert.Null(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void CanDeserialize_ComplexType_ThrowsFromConstructor()
    {
        // Arrange
        var expected = new ThrowsWithMissingParameterValue("Age");
        var data = new Dictionary<string, StringValues>() { ["value"] = "20" };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };

        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<ThrowsWithMissingParameterValue>(reader, options);

        // Assert
        Assert.Null(result);
        Assert.Equal(2, errors.Count);
        var error = errors[0];
        Assert.Equal("key", error.Key);
        Assert.Equal("Missing required value for constructor parameter 'key'.", error.Message.ToString(CultureInfo.InvariantCulture));

        var constructorError = errors[1];
        Assert.Equal("", constructorError.Key);
        Assert.Equal("Value cannot be null. (Parameter 'key')", constructorError.Message.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanSerializerFormFile()
    {
        // Arrange
        var expected = new FormFile(Stream.Null, 0, 10, "file", "file.txt");
        var formFileCollection = new FormFileCollection { expected };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, formFileCollection);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IFormFile));

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanSerializerIReadOnlyListFormFile()
    {
        // Arrange
        var formFileCollection = new FormFileCollection
        {
            new FormFile(Stream.Null, 0, 10, "file", "file-1.txt"),
            new FormFile(Stream.Null, 0, 20, "file", "file-2.txt"),
            new FormFile(Stream.Null, 0, 30, "file", "file-3.txt"),
            new FormFile(Stream.Null, 0, 40, "oddOneOutFile", "file-4.txt"),
        };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, formFileCollection);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IReadOnlyList<IFormFile>));

        // Assert
        var formFileResult = Assert.IsAssignableFrom<IReadOnlyList<IFormFile>>(result);
        Assert.Collection(formFileResult,
            element => Assert.Equal("file-1.txt", element.FileName),
            element => Assert.Equal("file-2.txt", element.FileName),
            element => Assert.Equal("file-3.txt", element.FileName)
        );
    }

    [Fact]
    public void CanDeserialize_ComplexType_ReturnsFirstFileForMultiples()
    {
        // Arrange
        var formFileCollection = new FormFileCollection
        {
            new FormFile(Stream.Null, 0, 10, "file", "file-1.txt"),
            new FormFile(Stream.Null, 0, 20, "file", "file-2.txt"),
            new FormFile(Stream.Null, 0, 30, "file", "file-3.txt"),
            new FormFile(Stream.Null, 0, 40, "oddOneOutFile", "file-4.txt"),
        };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, formFileCollection);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IFormFile));

        // Assert
        Assert.Equal(formFileCollection[0], result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanSerializerFormFileCollection()
    {
        // Arrange
        var expected = new FormFileCollection { new FormFile(Stream.Null, 0, 10, "file", "file.txt") };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, expected);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IFormFileCollection));

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanSerializerBrowserFile()
    {
        // Arrange
        var expectedString = "This is the contents of my text file.";
        var expected = new FormFileCollection { new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(expectedString)), 0, expectedString.Length, "file", "file.txt") };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, expected);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IBrowserFile));

        // Assert
        var browserFile = Assert.IsAssignableFrom<IBrowserFile>(result);
        Assert.Equal("file", browserFile.Name);
        Assert.Equal(expectedString.Length, browserFile.Size);
        var buffer = new byte[browserFile.Size];
        browserFile.OpenReadStream().Read(buffer);
        Assert.Equal(expectedString, Encoding.UTF8.GetString(buffer, 0, buffer.Length));
    }

    [Fact]
    public void CanDeserialize_ComplexType_CanSerializerIReadOnlyListBrowserFile()
    {
        // Arrange
        var expectedString1 = "This is the contents of my first text file.";
        var expectedString2 = "This is the contents of my second text file.";
        var expected = new FormFileCollection
        {
            new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(expectedString1)), 0, expectedString1.Length, "file", "file1.txt"),
            new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(expectedString2)), 0, expectedString2.Length, "file", "file2.txt")
        };
        var data = new Dictionary<string, StringValues>();
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture, expected);
        var errors = new List<FormDataMappingError>();
        reader.ErrorHandler = (key, message, attemptedValue) =>
        {
            errors.Add(new FormDataMappingError(key, message, attemptedValue));
        };
        reader.PushPrefix("file");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, typeof(IReadOnlyList<IBrowserFile>));

        // Assert
        var browserFiles = Assert.IsAssignableFrom<IReadOnlyList<IBrowserFile>>(result);
        // First file
        var browserFile1 = browserFiles[0];
        Assert.Equal("file", browserFile1.Name);
        Assert.Equal(expectedString1.Length, browserFile1.Size);
        var buffer1 = new byte[browserFile1.Size];
        browserFile1.OpenReadStream().Read(buffer1);
        Assert.Equal(expectedString1, Encoding.UTF8.GetString(buffer1, 0, buffer1.Length));
        // Second files
        var browserFile2 = browserFiles[0];
        Assert.Equal("file", browserFile2.Name);
        Assert.Equal(expectedString1.Length, browserFile2.Size);
        var buffer2 = new byte[browserFile2.Size];
        browserFile1.OpenReadStream().Read(buffer2);
        Assert.Equal(expectedString1, Encoding.UTF8.GetString(buffer2, 0, buffer2.Length));
    }

    [Fact]
    public void RecursiveTypes_Comparer_SortsValues_Correctly()
    {
        // Arrange
        var data = new Dictionary<string, StringValues>()
        {
            ["customerId"] = "20",
            ["customer[Id]"] = "20",
            ["customer.Id"] = "20"
        };
        var reader = CreateFormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("customer");

        // Act
        var result = reader.CurrentPrefixExists();

        // Assert
        Assert.True(result);
    }

    public static TheoryData<string, Type, object> NullableBasicTypes
    {
        get
        {
            var result = new TheoryData<string, Type, object>
            {
                // strings
                { "C", typeof(char?), new char?('C')},
                // bool
                { "true", typeof(bool?), new bool?(true)},
                // bytes
                { "63", typeof(byte?), new byte?((byte)0b_0011_1111)},
                { "-63", typeof(sbyte?), new sbyte?((sbyte)-0b_0011_1111)},
                // numeric types
                { "123", typeof(ushort?), new ushort?((ushort)123u)},
                { "456", typeof(uint?), new uint?(456u)},
                { "789", typeof(ulong?), new ulong?(789uL)},
                { "-101112", typeof(Int128?), new Int128?(-(Int128)101112)},
                { "-123", typeof(short?), new short?((short)-123)},
                { "-456", typeof(int?), new int?(-456)},
                { "-789", typeof(long?), new long?(-789L)},
                { "101112", typeof(UInt128?), new UInt128?((UInt128)101112)},
                // floating point types
                { "12.56", typeof(Half?), new Half?((Half)12.56f)},
                { "6.28", typeof(float?), new float?(6.28f)},
                { "3.14", typeof(double?), new double?(3.14)},
                { "1.23", typeof(decimal?), new decimal?(1.23m)},
                // dates and times
                { "04/20/2023", typeof(DateOnly?), new DateOnly?(new DateOnly(2023, 04, 20))},
                { "4/20/2023 12:56:34", typeof(DateTime?), new DateTime?(new DateTime(2023, 04, 20, 12, 56, 34))},
                { "4/20/2023 12:56:34 PM +02:00", typeof(DateTimeOffset?), new DateTimeOffset?(new DateTimeOffset(2023, 04, 20, 12, 56, 34, TimeSpan.FromHours(2)))},
                { "02:01:03", typeof(TimeSpan?), new TimeSpan?(new TimeSpan(02, 01, 03))},
                { "12:56:34", typeof(TimeOnly?), new TimeOnly?(new TimeOnly(12, 56, 34))},

                // other types
                { "a55eb3df-e984-42b5-85ca-4f68da8567d1", typeof(Guid?), new Guid?(new Guid("a55eb3df-e984-42b5-85ca-4f68da8567d1")) },
            };

            return result;
        }
    }

    public static TheoryData<Type> NullNullableBasicTypes
    {
        get
        {
            var result = new TheoryData<Type>
            {
                // strings
                { typeof(char?) },
                // bool
                { typeof(bool?) },
                // bytes
                { typeof(byte?) },
                { typeof(sbyte?) },
                // numeric types
                { typeof(ushort?) },
                { typeof(uint?) },
                { typeof(ulong?) },
                { typeof(Int128?) },
                { typeof(short?) },
                { typeof(int?) },
                { typeof(long?) },
                { typeof(UInt128?) },
                // floating point types
                { typeof(Half?) },
                { typeof(float?) },
                { typeof(double?) },
                { typeof(decimal?) },
                // dates and times
                { typeof(DateOnly?) },
                { typeof(DateTime?) },
                { typeof(DateTimeOffset?) },
                { typeof(TimeSpan?) },
                { typeof(TimeOnly?) },

                // other types
                { typeof(Guid?) },
            };

            return result;
        }
    }

    public static TheoryData<string, Type, object> PrimitiveTypesData
    {
        get
        {
            var result = new TheoryData<string, Type, object>
            {
                // strings
                { "C", typeof(char), 'C' },
                { "hello", typeof(string), "hello" },
                // bool
                { "true", typeof(bool), true },
                // bytes
                { "63", typeof(byte), (byte)0b_0011_1111 },
                { "-63", typeof(sbyte), (sbyte)-0b_0011_1111 },
                // numeric types
                { "123", typeof(ushort), (ushort)123u },
                { "456", typeof(uint), 456u },
                { "789", typeof(ulong), 789uL },
                { "-101112", typeof(Int128), -(Int128)101112 },
                { "-123", typeof(short), (short)-123 },
                { "-456", typeof(int), -456 },
                { "-789", typeof(long), -789L },
                { "101112", typeof(UInt128), (UInt128)101112 },
                // floating point types
                { "12.56", typeof(Half), (Half)12.56f },
                { "6.28", typeof(float), 6.28f },
                { "3.14", typeof(double), 3.14 },
                { "1.23", typeof(decimal), 1.23m },
                // dates and times
                { "04/20/2023", typeof(DateOnly), new DateOnly(2023, 04, 20) },
                { "4/20/2023 12:56:34", typeof(DateTime), new DateTime(2023, 04, 20, 12, 56, 34) },
                { "4/20/2023 12:56:34 PM +02:00", typeof(DateTimeOffset), new DateTimeOffset(2023, 04, 20, 12, 56, 34, TimeSpan.FromHours(2)) },
                { "02:01:03", typeof(TimeSpan), new TimeSpan(02, 01, 03) },
                { "12:56:34", typeof(TimeOnly), new TimeOnly(12, 56, 34) },

                // other types
                { "a55eb3df-e984-42b5-85ca-4f68da8567d1", typeof(Guid), new Guid("a55eb3df-e984-42b5-85ca-4f68da8567d1") }
            };

            return result;
        }
    }

    private object CallDeserialize(FormDataReader reader, FormDataMapperOptions options, Type type)
    {
        var method = typeof(FormDataMapper)
            .GetMethod("Map", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) ??
            throw new InvalidOperationException("Unable to find method 'Map'.");

        return method.MakeGenericMethod(type).Invoke(null, new object[] { reader, options })!;
    }
}

internal class TypeWithUri
{
    public Uri Slug { get; set; }
}

internal class Point : IParsable<Point>, IEquatable<Point>
{
    public int X { get; set; }
    public int Y { get; set; }

    public static Point Parse(string s, IFormatProvider provider)
    {
        // Parses points. Points start with ( and end with ).
        // Points define two components, X and Y, separated by a comma.
        var components = s.Trim('(', ')').Split(',');
        if (components.Length != 2)
        {
            throw new FormatException("Invalid point format.");
        }
        var result = new Point();
        result.X = int.Parse(components[0], provider);
        result.Y = int.Parse(components[1], provider);
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, [MaybeNullWhen(false)] out Point result)
    {
        // Try parse points is similar to Parse, but returns a bool to indicate success.
        // It also uses the out parameter to return the result.
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    public override bool Equals(object obj) => Equals(obj as Point);

    public bool Equals(Point other) => other is not null && X == other.X && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Point left, Point right) => EqualityComparer<Point>.Default.Equals(left, right);

    public static bool operator !=(Point left, Point right) => !(left == right);
}

internal struct ValuePoint : IParsable<ValuePoint>, IEquatable<ValuePoint>
{
    public int X { get; set; }

    public int Y { get; set; }

    public static ValuePoint Parse(string s, IFormatProvider provider)
    {
        // Parses points. Points start with ( and end with ).
        // Points define two components, X and Y, separated by a comma.
        var components = s.Trim('(', ')').Split(',');
        if (components.Length != 2)
        {
            throw new FormatException("Invalid point format.");
        }
        var result = new ValuePoint();
        result.X = int.Parse(components[0], provider);
        result.Y = int.Parse(components[1], provider);
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, [MaybeNullWhen(false)] out ValuePoint result)
    {
        // Try parse points is similar to Parse, but returns a bool to indicate success.
        // It also uses the out parameter to return the result.
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch (FormatException)
        {
            result = default;
            return false;
        }
    }

    public override bool Equals(object obj) => Equals((ValuePoint)obj);

    public bool Equals(ValuePoint other) => X == other.X && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(ValuePoint left, ValuePoint right) => EqualityComparer<ValuePoint>.Default.Equals(left, right);

    public static bool operator !=(ValuePoint left, ValuePoint right) => !(left == right);
}

internal abstract class TestArrayPoolBufferAdapter
    : ICollectionBufferAdapter<int[], TestArrayPoolBufferAdapter.PooledBuffer, int>
{
    public static event Action<int[]> OnRent;
    public static event Action<int[]> OnReturn;

    public static PooledBuffer CreateBuffer() => new() { Data = Rent(16), Count = 0 };

    public static PooledBuffer Add(ref PooledBuffer buffer, int element)
    {
        if (buffer.Count >= buffer.Data.Length)
        {
            var newBuffer = Rent(buffer.Data.Length * 2);
            Array.Copy(buffer.Data, newBuffer, buffer.Data.Length);
            Return(buffer.Data);
            buffer.Data = newBuffer;
        }

        buffer.Data[buffer.Count++] = element;
        return buffer;
    }

    public static int[] ToResult(PooledBuffer buffer)
    {
        var result = new int[buffer.Count];
        Array.Copy(buffer.Data, result, buffer.Count);
        Return(buffer.Data);
        return result;
    }

    public struct PooledBuffer
    {
        public int[] Data { get; set; }
        public int Count { get; set; }
    }

    public static int[] Rent(int size)
    {
        var result = ArrayPool<int>.Shared.Rent(size);
        OnRent?.Invoke(result);
        return result;
    }

    public static void Return(int[] array)
    {
        OnReturn?.Invoke(array);
        ArrayPool<int>.Shared.Return(array);
    }
}

public enum Colors
{
    Red,
    Green,
    Blue
}

internal struct Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public int ZipCode { get; set; }
}

internal class Customer
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public bool IsPreferred { get; set; }
}

internal class FrequentCustomer : Customer
{
    public int TotalVisits { get; set; }

    public string PreferredStore { get; set; }

    public double MonthlyFrequency { get; set; }
}

// Implements ICollection<TEnum> delegating to List<TEnum> _inner;
internal class CustomCollection<T> : ICollection<T>
{
    private readonly List<T> _inner = new();

    public int Count => _inner.Count;
    public bool IsReadOnly => false;
    public void Add(T item) => _inner.Add(item);
    public void Clear() => _inner.Clear();
    public bool Contains(T item) => _inner.Contains(item);
    public bool Remove(T item) => _inner.Remove(item);
    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
}

internal class RecursiveList
{
    public int Head { get; set; }
    public RecursiveList Tail { get; set; }
}

internal class RecursiveTree
{
    public int Value { get; set; }
    public List<RecursiveTree> Children { get; set; }
}

internal class RecursiveDictionaryTree
{
    public int Value { get; set; }

    public Dictionary<int, RecursiveDictionaryTree> Children { get; set; }
}

internal record ClassRecordType(string Key, int Value);

internal record struct StructRecordType(string Key, int Value);

internal class DataMemberAttributesType
{
    [DataMember(Name = "mycustomkey")]
    public string Key { get; set; }

    [DataMember(Name = "mycustomvalue")]
    public int Value { get; set; }

    [IgnoreDataMember]
    public string Ignored { get; set; }
}

internal class DataMemberAttributesConstructorType
{
    public DataMemberAttributesConstructorType(string key, int value)
    {
        Key = key;
        Value = value;
    }

    [DataMember(Name = "mycustomkey")]
    public string Key { get; set; }

    [DataMember(Name = "mycustomvalue")]
    public int Value { get; set; }

    [IgnoreDataMember]
    public string Ignored { get; set; }
}

internal class TypeIgnoresReadOnlyProperties
{
    public int Id { get; }

    public int Age { get; internal set; }

    public string Email { get; private set; }

    public bool IsPreferred { get; protected set; }

    public string Name { get; set; }
}

internal class TypeRequiredProperties
{
    public int Age { get; set; }

    public required string Name { get; set; }
}

public class ThrowsWithMissingParameterValue
{
    public ThrowsWithMissingParameterValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
    }

    public string Key { get; set; }

    public int Value { get; set; }
}

public class Throwing { }

internal class ThrowingConverter : FormDataConverter<int>
{
    internal delegate bool OnTryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out int result, out bool found);

    internal OnTryRead OnTryReadDelegate { get; set; } =
        (ref FormDataReader context, Type type, FormDataMapperOptions options, out int result, out bool found) =>
            throw new InvalidOperationException("Could not read value.");

    internal override bool TryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out int result, out bool found) =>
        OnTryReadDelegate.Invoke(ref context, type, options, out result, out found);
}
