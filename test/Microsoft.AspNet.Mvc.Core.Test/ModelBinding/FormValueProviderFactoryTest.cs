// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class FormValueProviderFactoryTest
    {
        [Fact]
        public async Task GetValueProviderAsync_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
        {
            // Arrange
            var context = CreateContext("some-content-type");
            var factory = new FormValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq; charset=utf-8")]
        public async Task GetValueProviderAsync_ReturnsValueProvider_WithCurrentCulture(string contentType)
        {
            // Arrange
            var context = CreateContext(contentType);
            var factory = new FormValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(context);

            // Assert
            var valueProvider = Assert.IsType<FormValueProvider>(result);
            Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
        }

        private static ActionContext CreateContext(string contentType)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;

            if (context.Request.HasFormContentType)
            {
                context.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            }

            return new ActionContext(context, new RouteData(), new ActionDescriptor());
        }
    }
}
