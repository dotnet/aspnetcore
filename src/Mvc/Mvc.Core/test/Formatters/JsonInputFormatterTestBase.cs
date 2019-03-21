// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public abstract class JsonInputFormatterTestBase
    {
        [Theory]
        [InlineData("application/json", true)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        [InlineData("text/json", true)]
        [InlineData("text/*", false)]
        [InlineData("text/xml", false)]
        [InlineData("application/xml", false)]
        [InlineData("application/some.entity+json", true)]
        [InlineData("application/some.entity+json;v=2", true)]
        [InlineData("application/some.entity+xml", false)]
        [InlineData("application/some.entity+*", false)]
        [InlineData("text/some.entity+json", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes("content");
            var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);

            var formatterContext = CreateInputFormatterContext(typeof(string), httpContext);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Fact]
        public void DefaultMediaType_ReturnsApplicationJson()
        {
            // Arrange
            var formatter = GetInputFormatter();

            // Act
            var mediaType = formatter.SupportedMediaTypes[0];

            // Assert
            Assert.Equal("application/json", mediaType.ToString());
        }

        [Fact]
        public async Task JsonFormatterReadsIntValue()
        {
            // Arrange
            var content = "100";
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(int), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var intValue = Assert.IsType<int>(result.Model);
            Assert.Equal(100, intValue);
        }

        [Fact]
        public async Task JsonFormatterReadsStringValue()
        {
            // Arrange
            var content = "\"abcd\"";
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(string), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var stringValue = Assert.IsType<string>(result.Model);
            Assert.Equal("abcd", stringValue);
        }

        [Fact]
        public virtual async Task JsonFormatterReadsDateTimeValue()
        {
            // Arrange
            var content = "\"2012-02-01 12:45 AM\"";
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(DateTime), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var dateValue = Assert.IsType<DateTime>(result.Model);
            Assert.Equal(new DateTime(2012, 02, 01, 00, 45, 00), dateValue);
        }

        [Fact]
        public async Task JsonFormatterReadsComplexTypes()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "{\"Name\": \"Person Name\", \"Age\": 30}";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<ComplexModel>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);
        }

        [Fact]
        public async Task ReadAsync_ReadsValidArray()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "[0, 23, 300]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(int[]), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var integers = Assert.IsType<int[]>(result.Model);
            Assert.Equal(new int[] { 0, 23, 300 }, integers);
        }

        [Fact]
        public virtual Task ReadAsync_ReadsValidArray_AsListOfT() => ReadAsync_ReadsValidArray_AsList(typeof(List<int>));

        [Fact]
        public virtual Task ReadAsync_ReadsValidArray_AsIListOfT() => ReadAsync_ReadsValidArray_AsList(typeof(IList<int>));

        [Fact]
        public virtual Task ReadAsync_ReadsValidArray_AsCollectionOfT() => ReadAsync_ReadsValidArray_AsList(typeof(ICollection<int>));

        [Fact]
        public virtual Task ReadAsync_ReadsValidArray_AsEnumerableOfT() => ReadAsync_ReadsValidArray_AsList(typeof(IEnumerable<int>));

        protected async Task ReadAsync_ReadsValidArray_AsList(Type requestedType)
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "[0, 23, 300]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(requestedType, httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var integers = Assert.IsType<List<int>>(result.Model);
            Assert.Equal(new int[] { 0, 23, 300 }, integers);
        }

        [Fact]
        public virtual async Task ReadAsync_AddsModelValidationErrorsToModelState()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "{ \"Name\": \"Person Name\", \"Age\": \"not-an-age\" }";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Could not convert string to decimal: not-an-age. Path 'Age', line 1, position 44.",
                formatterContext.ModelState["Age"].Errors[0].ErrorMessage);
        }

        [Fact]
        public virtual async Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "[0, 23, 300]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(byte[]), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal("The supplied value is invalid.", formatterContext.ModelState["[2]"].Errors[0].ErrorMessage);
            Assert.Null(formatterContext.ModelState["[2]"].Errors[0].Exception);
        }

        [Fact]
        public virtual async Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "[{ \"Name\": \"Name One\", \"Age\": 30}, { \"Name\": \"Name Two\", \"Small\": 300}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(ComplexModel[]), httpContext, modelName: "names");

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Error converting value 300 to type 'System.Byte'. Path '[1].Small', line 1, position 69.",
                formatterContext.ModelState["names[1].Small"].Errors[0].ErrorMessage);
        }

        [Fact]
        public virtual async Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
        {
            // Arrange
            var formatter = GetInputFormatter();

            var content = "{ \"Name\": \"Person Name\", \"Age\": \"not-an-age\"}";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(ComplexModel), httpContext);
            formatterContext.ModelState.MaxAllowedErrors = 3;
            formatterContext.ModelState.AddModelError("key1", "error1");
            formatterContext.ModelState.AddModelError("key2", "error2");

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.True(result.HasError);

            Assert.False(formatterContext.ModelState.ContainsKey("age"));
            var error = Assert.Single(formatterContext.ModelState[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
        }

        [Theory]
        [InlineData("null", true, true)]
        [InlineData("null", false, false)]
        public async Task ReadAsync_WithInputThatDeserializesToNull_SetsModelOnlyIfAllowingEmptyInput(
            string content,
            bool treatEmptyInputAsDefaultValue,
            bool expectedIsModelSet)
        {
            // Arrange
            var formatter = GetInputFormatter();

            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = GetHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(
                typeof(string),
                httpContext,
                treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            Assert.Equal(expectedIsModelSet, result.IsModelSet);
            Assert.Null(result.Model);
        }

        protected abstract TextInputFormatter GetInputFormatter();

        protected static HttpContext GetHttpContext(
            byte[] contentBytes,
            string contentType = "application/json")
        {
            return GetHttpContext(new MemoryStream(contentBytes), contentType);
        }

        protected static HttpContext GetHttpContext(
            Stream requestStream,
            string contentType = "application/json")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = requestStream;
            httpContext.Request.ContentType = contentType;

            return httpContext;
        }

        protected static InputFormatterContext CreateInputFormatterContext(
            Type modelType,
            HttpContext httpContext,
            string modelName = null,
            bool treatEmptyInputAsDefaultValue = false)
        {
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelType);

            return new InputFormatterContext(
                httpContext,
                modelName: modelName ?? string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader,
                treatEmptyInputAsDefaultValue: treatEmptyInputAsDefaultValue);
        }

        protected sealed class ComplexModel
        {
            public string Name { get; set; }

            public decimal Age { get; set; }

            public byte Small { get; set; }
        }
    }
}
