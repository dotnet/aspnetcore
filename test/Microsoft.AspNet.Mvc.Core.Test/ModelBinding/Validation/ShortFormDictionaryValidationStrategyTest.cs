// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;


namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ShortFormDictionaryValidationStrategyTest
    {
        [Fact]
        public void EnumerateElements()
        {
            // Arrange
            var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
                { 5, "five" },
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
            var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
            var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "2", 2 },
                { "3", 3 },
                { "5", 5 },
            },
            valueMetadata);

            // Act
            var enumerator = strategy.GetChildren(metadata, "prefix", model);

            // Assert
            Assert.Collection(
                BufferEntries(enumerator).OrderBy(e => e.Key),
                e =>
                {
                    Assert.Equal("prefix[2]", e.Key);
                    Assert.Equal("two", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix[3]", e.Key);
                    Assert.Equal("three", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix[5]", e.Key);
                    Assert.Equal("five", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                });
        }

        [Fact]
        public void EnumerateElements_RunOutOfIndices()
        {
            // Arrange
            var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
                { 5, "five" },
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
            var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
            var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "2", 2 },
                { "3", 3 },
            },
            valueMetadata);

            // Act
            var enumerator = strategy.GetChildren(metadata, "prefix", model);

            // Assert
            Assert.Collection(
                BufferEntries(enumerator).OrderBy(e => e.Key),
                e =>
                {
                    Assert.Equal("prefix[2]", e.Key);
                    Assert.Equal("two", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix[3]", e.Key);
                    Assert.Equal("three", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                });
        }

        [Fact]
        public void EnumerateElements_RunOutOfElements()
        {
            // Arrange
            var model = new Dictionary<int, string>()
            {
                { 2, "two" },
                { 3, "three" },
            };

            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var metadata = metadataProvider.GetMetadataForType(typeof(List<int>));
            var valueMetadata = metadataProvider.GetMetadataForType(typeof(string));
            var strategy = new ShortFormDictionaryValidationStrategy<int, string>(new Dictionary<string, int>()
            {
                { "2", 2 },
                { "3", 3 },
                { "5", 5 },
            },
            valueMetadata);

            // Act
            var enumerator = strategy.GetChildren(metadata, "prefix", model);

            // Assert
            Assert.Collection(
                BufferEntries(enumerator).OrderBy(e => e.Key),
                e =>
                {
                    Assert.Equal("prefix[2]", e.Key);
                    Assert.Equal("two", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix[3]", e.Key);
                    Assert.Equal("three", e.Model);
                    Assert.Same(valueMetadata, e.Metadata);
                });
        }

        private List<ValidationEntry> BufferEntries(IEnumerator<ValidationEntry> enumerator)
        {
            var entries = new List<ValidationEntry>();
            while (enumerator.MoveNext())
            {
                entries.Add(enumerator.Current);
            }

            return entries;
        }
    }
}
