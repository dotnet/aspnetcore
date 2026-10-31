// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class FormatterCollectionTest
{
    [Fact]
    public void NonGenericRemoveType_RemovesAllOfType()
    {
        // Arrange
        var collection = new FormatterCollection<IOutputFormatter>
            {
                new TestOutputFormatter(),
                new AnotherTestOutputFormatter(),
                new TestOutputFormatter()
            };

        // Act
        collection.RemoveType(typeof(TestOutputFormatter));

        // Assert
        var formatter = Assert.Single(collection);
        Assert.IsType<AnotherTestOutputFormatter>(formatter);
    }

    [Fact]
    public void RemoveType_RemovesAllOfType()
    {
        // Arrange
        var collection = new FormatterCollection<IOutputFormatter>
            {
                new TestOutputFormatter(),
                new AnotherTestOutputFormatter(),
                new TestOutputFormatter()
            };

        // Act
        collection.RemoveType<TestOutputFormatter>();

        // Assert
        var formatter = Assert.Single(collection);
        Assert.IsType<AnotherTestOutputFormatter>(formatter);
    }

    private class TestOutputFormatter : TextOutputFormatter
    {
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            throw new NotImplementedException();
        }
    }

    private class AnotherTestOutputFormatter : TextOutputFormatter
    {
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            throw new NotImplementedException();
        }
    }
}
