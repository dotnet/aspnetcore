// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class  OpenApiSchemaGeneratorTests
{
    [Theory]
    [InlineData(typeof(Dictionary<string, string>))]
    [InlineData(typeof(Todo))]
    public void CanGenerateCorrectSchemaForDictionaryTypes(Type type)
    {
        var schema = OpenApiSchemaGenerator.GetOpenApiSchema(type);
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
    }

    [Theory]
    [InlineData(typeof(IList<string>))]
    [InlineData(typeof(Products))]
    public void CanGenerateSchemaForListTypes(Type type)
    {
        var schema = OpenApiSchemaGenerator.GetOpenApiSchema(type);
        Assert.NotNull(schema);
        Assert.Equal("array", schema.Type);
    }

    [Theory]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateTimeOffset))]
    public void CanGenerateSchemaForDateTimeTypes(Type type)
    {
        var schema = OpenApiSchemaGenerator.GetOpenApiSchema(type);
        Assert.NotNull(schema);
        Assert.Equal("string", schema.Type);
        Assert.Equal("date-time", schema.Format);
    }

    [Fact]
    public void CanGenerateSchemaForDateSpanTypes()
    {
        var schema = OpenApiSchemaGenerator.GetOpenApiSchema(typeof(TimeSpan));
        Assert.NotNull(schema);
        Assert.Equal("string", schema.Type);
        Assert.Equal("date-span", schema.Format);
    }

    class Todo : Dictionary<string, object> { }
    class Products : IList<int>
    {
        public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
