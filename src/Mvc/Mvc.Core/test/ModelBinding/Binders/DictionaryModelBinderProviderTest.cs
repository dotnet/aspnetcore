// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class DictionaryModelBinderProviderTest
    {
        [Theory]
        [InlineData(typeof(Person))]
        [InlineData(typeof(string))]
        [InlineData(typeof(IEnumerable<KeyValuePair<string, int>>))]
        [InlineData(typeof(ICollection<string>))]
        public void Create_ForNonDictionaryType_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new DictionaryModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(typeof(IDictionary<string, int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        public void Create_ForDictionaryType_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new DictionaryModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);
            context.OnCreatingBinder(m =>
            {
                if (m.ModelType == typeof(KeyValuePair<string, int>) ||
                    m.ModelType == typeof(int) ||
                    m.ModelType == typeof(string))
                {
                    return Mock.Of<IModelBinder>();
                }
                else
                {
                    Assert.False(true, "Not the right model type");
                    return null;
                }
            });

            // Act
            var result = provider.GetBinder(context);

            // Assert
            var binder = Assert.IsType<DictionaryModelBinder<string, int>>(result);
            Assert.False(binder.AllowValidatingTopLevelNodes); // work done in DictionaryModelBinder.
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
