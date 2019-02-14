// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class ExcludeBindingMetadataProviderTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsBindingAllowed_LeftAlone_WhenTypeDoesntMatch(bool initialValue)
        {
            // Arrange
            var provider = new ExcludeBindingMetadataProvider(typeof(string));

            var key = ModelMetadataIdentity.ForProperty(
                typeof(int),
                nameof(Person.Age),
                typeof(Person));

            var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

            context.BindingMetadata.IsBindingAllowed = initialValue;

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsBindingAllowed_IsFalse_WhenTypeMatches(bool initialValue)
        {
            // Arrange
            var provider = new ExcludeBindingMetadataProvider(typeof(int));

            var key = ModelMetadataIdentity.ForProperty(
                typeof(int),
                nameof(Person.Age),
                typeof(Person));

            var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

            context.BindingMetadata.IsBindingAllowed = initialValue;

            // Act
            provider.CreateBindingMetadata(context);

            // Assert
            Assert.False(context.BindingMetadata.IsBindingAllowed);
        }

        private class Person
        {
            public int Age { get; set; }
        }
    }
}
