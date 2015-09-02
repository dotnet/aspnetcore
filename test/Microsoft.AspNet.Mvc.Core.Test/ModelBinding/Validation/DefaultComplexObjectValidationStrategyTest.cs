// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class DefaultComplexObjectValidationStrategyTest
    {
        [Fact]
        public void EnumerateElements()
        {
            // Arrange
            var model = new Person()
            {
                Age = 23,
                Id = 1,
                Name = "Joey",
            };

            var metadata = TestModelMetadataProvider.CreateDefaultProvider().GetMetadataForType(typeof(Person));
            var strategy = DefaultComplexObjectValidationStrategy.Instance;

            // Act
            var enumerator = strategy.GetChildren(metadata, "prefix", model);

            // Assert
            Assert.Collection(
                BufferEntries(enumerator).OrderBy(e => e.Key),
                e => 
                {
                    Assert.Equal("prefix.Age", e.Key);
                    Assert.Equal(23, e.Model);
                    Assert.Same(metadata.Properties["Age"], e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix.Id", e.Key);
                    Assert.Equal(1, e.Model);
                    Assert.Same(metadata.Properties["Id"], e.Metadata);
                },
                e =>
                {
                    Assert.Equal("prefix.Name", e.Key);
                    Assert.Equal("Joey", e.Model);
                    Assert.Same(metadata.Properties["Name"], e.Metadata);
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

        private class Person
        {
            public int Id { get; set; }

            public int Age { get; set; }

            public string Name { get; set; }
        }
    }
}
