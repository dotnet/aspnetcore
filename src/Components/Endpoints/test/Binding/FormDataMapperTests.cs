// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class FormDataMapperTests
{
    [Theory]
    [MemberData(nameof(PrimitiveTypesData))]
    public void CanDeserialize_PrimitiveTypes(string value, Type type, object expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(NullableBasicTypes))]
    public void CanDeserialize_NullablePrimitiveTypes(string value, Type type, object expected)
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues(value) };
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = CallDeserialize(reader, options, type);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CanDeserialize_CustomParsableTypes()
    {
        // Arrange
        var expected = new Point { X = 1, Y = 1 };
        var collection = new Dictionary<string, StringValues>() { ["value"] = new StringValues("(1,1)") };
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        var value = Assert.Single(result);
        Assert.Equal(10, value);
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

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
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

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = 110
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
    [InlineData(110)]
    [InlineData(120)]
    public void Deserialize_Collections_ParsesUpToMaxCollectionSize(int size)
    {
        // Arrange
        var data = new Dictionary<string, StringValues>(Enumerable.Range(0, size)
            .Select(i => new KeyValuePair<string, StringValues>(
                $"[{i.ToString(CultureInfo.InvariantCulture)}]",
                (i + 10).ToString(CultureInfo.InvariantCulture))));

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
        var options = new FormDataMapperOptions
        {
            MaxCollectionSize = 110
        };

        // Act
        var result = FormDataMapper.Map<List<int>>(reader, options);

        // Assert
        Assert.Equal(110, result.Count);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<IReadOnlyDictionary<int, int>>(reader, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_Dictionary_RespectsMaxCollectionSize()
    {
        // Arrange
        var collection = new Dictionary<string, StringValues>() { };
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
        var options = new FormDataMapperOptions();

        // Act
        var result = FormDataMapper.Map<IReadOnlyDictionary<int, int>>(reader, options);

        // Assert
        Assert.Null(result);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
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
        var reader = new FormDataReader(collection, CultureInfo.InvariantCulture);
        reader.PushPrefix("value");
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
        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
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

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
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
    public void CanDeserialize_ComplexReferenceType_Inheritance()
    {
        // Arrange
        var expected = new FrequentCustomer() { Age = 20, Name = "John Doe", Email = "john@example.com"        , IsPreferred = true, TotalVisits = 10, PreferredStore = "Redmond", MonthlyFrequency = 0.8 };
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

        var reader = new FormDataReader(data, CultureInfo.InvariantCulture);
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

// Implements ICollection<T> delegating to List<T> _inner;
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
