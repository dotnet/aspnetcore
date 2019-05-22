// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class FormValueProviderFactoryTest
    {
        [Fact]
        public async Task GetValueProviderAsync_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
        {
            // Arrange
            var context = CreateContext("some-content-type", true);
            var factory = new FormValueProviderFactory();

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            Assert.Empty(context.ValueProviders);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq; charset=utf-8")]
        public async Task GetValueProviderAsync_ReturnsValueProvider_WithCurrentCulture(string contentType)
        {
            // Arrange
            var context = CreateContext(contentType, true);
            var factory = new FormValueProviderFactory();

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            var valueProvider = Assert.IsType<FormValueProvider>(Assert.Single(context.ValueProviders));
            Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
        }

        [Fact]
        public async Task GetValueProviderAsync_AddErrorToModelState_IfMultipartFormdataIsIncorrect()
        {
            // Arrange
            var context = CreateContext("multipart/form-data", false);
            var factory = new FormValueProviderFactory();

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            var modelState = context.ActionContext.ModelState;
            Assert.False(modelState.IsValid);
            Assert.Equal("", modelState.Single().Key);
            var invalidDataException = Assert.IsType<InvalidDataException>(modelState.Single().Value.Errors.Single().Exception);
            Assert.Equal("Missing content-type boundary.", invalidDataException.Message);
        }

        private static ValueProviderFactoryContext CreateContext(string contentType, bool assignForm)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;

            if (context.Request.HasFormContentType && assignForm)
            {
                context.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            }

            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            return new ValueProviderFactoryContext(actionContext);
        }
    }
}
