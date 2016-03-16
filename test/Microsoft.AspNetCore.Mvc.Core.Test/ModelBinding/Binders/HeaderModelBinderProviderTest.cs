// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class HeaderModelBinderProviderTest
    {
        public static TheoryData<BindingSource> NonHeaderBindingSources
        {
            get
            {
                return new TheoryData<BindingSource>()
                {
                    BindingSource.Body,
                    BindingSource.Form,
                    null,
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonHeaderBindingSources))]
        public void Create_WhenBindingSourceIsNotFromHeader_ReturnsNull(BindingSource source)
        {
            // Arrange
            var provider = new HeaderModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(string));
            context.BindingInfo.BindingSource = source;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_WhenBindingSourceIsFromHeader_ReturnsBinder()
        {
            // Arrange
            var provider = new HeaderModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(string));
            context.BindingInfo.BindingSource = BindingSource.Header;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<HeaderModelBinder>(result);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(string[]))]
        [InlineData(typeof(Collection<string>))]
        public void Create_WhenModelTypeIsSupportedType_ReturnsBinder(Type modelType)
        {
            // Arrange
            var provider = new HeaderModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);
            context.BindingInfo.BindingSource = BindingSource.Header;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<HeaderModelBinder>(result);
        }

        [Theory]
        [InlineData(typeof(Dictionary<int, string>))]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(Person))]
        public void Create_WhenModelTypeIsUnsupportedType_ReturnsNull(Type modelType)
        {
            // Arrange
            var provider = new HeaderModelBinderProvider();

            var context = new TestModelBinderProviderContext(modelType);
            context.BindingInfo.BindingSource = BindingSource.Header;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
