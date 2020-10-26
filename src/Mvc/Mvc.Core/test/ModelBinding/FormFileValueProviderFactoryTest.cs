// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class FormFileValueProviderFactoryTest
    {
        [Fact]
        public async Task CreateValueProviderAsync_DoesNotAddValueProvider_IfRequestDoesNotHaveFormContent()
        {
            // Arrange
            var factory = new FormFileValueProviderFactory();
            var context = CreateContext("application/json");

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            Assert.Empty(context.ValueProviders);
        }

        [Fact]
        public async Task CreateValueProviderAsync_DoesNotAddValueProvider_IfFileCollectionIsEmpty()
        {
            // Arrange
            var factory = new FormFileValueProviderFactory();
            var context = CreateContext("multipart/form-data");

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            Assert.Empty(context.ValueProviders);
        }

        [Fact]
        public async Task CreateValueProviderAsync_AddsValueProvider()
        {
            // Arrange
            var factory = new FormFileValueProviderFactory();
            var context = CreateContext("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq");
            var files = (FormFileCollection)context.ActionContext.HttpContext.Request.Form.Files;
            files.Add(new FormFile(Stream.Null, 0, 10, "some-name", "some-name"));

            // Act
            await factory.CreateValueProviderAsync(context);

            // Assert
            Assert.Collection(
                context.ValueProviders,
                v => Assert.IsType<FormFileValueProvider>(v));
        }

        private static ValueProviderFactoryContext CreateContext(string contentType)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;
            context.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());

            return new ValueProviderFactoryContext(actionContext);
        }
    }
}
