// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class FormFileValueProviderTest
    {
        [Fact]
        public void ContainsPrefix_ReturnsFalse_IfFileIs0LengthAndFileNameIsEmpty()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "multipart/form-data";
            var formFiles = new FormFileCollection();
            formFiles.Add(new FormFile(Stream.Null, 0, 0, "file", fileName: null));
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), formFiles);

            var valueProvider = new FormFileValueProvider(formFiles);

            // Act
            var result = valueProvider.ContainsPrefix("file");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsPrefix_ReturnsTrue_IfFileExists()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "multipart/form-data";
            var formFiles = new FormFileCollection();
            formFiles.Add(new FormFile(Stream.Null, 0, 10, "file", "file"));
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), formFiles);

            var valueProvider = new FormFileValueProvider(formFiles);

            // Act
            var result = valueProvider.ContainsPrefix("file");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetValue_ReturnsNoneResult()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "multipart/form-data";
            var formFiles = new FormFileCollection();
            formFiles.Add(new FormFile(Stream.Null, 0, 10, "file", "file"));
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>(), formFiles);

            var valueProvider = new FormFileValueProvider(formFiles);

            // Act
            var result = valueProvider.GetValue("file");

            // Assert
            Assert.Equal(ValueProviderResult.None, result);
        }
    }
}
