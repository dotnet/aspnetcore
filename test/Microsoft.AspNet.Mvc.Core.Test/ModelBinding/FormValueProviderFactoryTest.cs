// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class FormValueProviderFactoryTest
    {
        [Fact]
        public void GetValueProvider_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
        {
            // Arrange
            var context = CreateContext("some-content-type");
            var factory = new FormValueProviderFactory();

            // Act
            var result = factory.GetValueProvider(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq; charset=utf-8")]
        public void GetValueProvider_ReturnsValueProviderInstanceWithCurrentCulture(string contentType)
        {
            // Arrange
            var context = CreateContext(contentType);
            var factory = new FormValueProviderFactory();

            // Act
            var result = factory.GetValueProvider(context);

            // Assert
            var valueProvider = Assert.IsType<ReadableStringCollectionValueProvider>(result);
            Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
        }

        private static ValueProviderFactoryContext CreateContext(string contentType)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;

            return new ValueProviderFactoryContext(
                context,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
