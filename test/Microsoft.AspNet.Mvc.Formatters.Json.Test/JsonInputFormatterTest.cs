// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class JsonInputFormatterTest
    {

        [Theory]
        [InlineData("application/json", true)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        [InlineData("text/json", true)]
        [InlineData("text/*", false)]
        [InlineData("text/xml", false)]
        [InlineData("application/xml", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes("content");

            var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(string));
            var formatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Fact]
        public void DefaultMediaType_ReturnsApplicationJson()
        {
            // Arrange
            var formatter = new JsonInputFormatter();

            // Act
            var mediaType = formatter.SupportedMediaTypes[0];

            // Assert
            Assert.Equal("application/json", mediaType.ToString());
        }

        public static IEnumerable<object[]> JsonFormatterReadSimpleTypesData
        {
            get
            {
                yield return new object[] { "100", typeof(int), 100 };
                yield return new object[] { "'abcd'", typeof(string), "abcd" };
                yield return new object[] { "'2012-02-01 12:45 AM'", typeof(DateTime),
                                            new DateTime(2012, 02, 01, 00, 45, 00) };
            }
        }

        [Theory]
        [MemberData(nameof(JsonFormatterReadSimpleTypesData))]
        public async Task JsonFormatterReadsSimpleTypes(string content, Type type, object expected)
        {
            // Arrange
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(type);
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            Assert.Equal(expected, result.Model);
        }

        [Fact]
        public async Task JsonFormatterReadsComplexTypes()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: '30'}";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: new ModelStateDictionary(),
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.False(result.HasError);
            var userModel = Assert.IsType<User>(result.Model);
            Assert.Equal("Person Name", userModel.Name);
            Assert.Equal(30, userModel.Age);
        }

        [Fact]
        public async Task ReadAsync_AddsModelValidationErrorsToModelState()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Could not convert string to decimal: not-an-age. Path 'Age', line 1, position 39.",
                modelState["Age"].Errors[0].Exception.Message);
        }

        [Fact]
        public async Task ReadAsync_InvalidArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var content = "[0, 23, 300]";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(byte[]));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal("The supplied value is invalid for Byte.", modelState["[2]"].Errors[0].ErrorMessage);
            Assert.Null(modelState["[2]"].Errors[0].Exception);
        }

        [Fact]
        public async Task ReadAsync_InvalidComplexArray_AddsOverflowErrorsToModelState()
        {
            // Arrange
            var content = "[{name: 'Name One', Age: 30}, {name: 'Name Two', Small: 300}]";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User[]));
            var context = new InputFormatterContext(
                httpContext,
                modelName: "names",
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.Equal(
                "Error converting value 300 to type 'System.Byte'. Path '[1].Small', line 1, position 59.",
                modelState["names[1].Small"].Errors[0].Exception.Message);
        }

        [Fact]
        public async Task ReadAsync_UsesTryAddModelValidationErrorsToModelState()
        {
            // Arrange
            var content = "{name: 'Person Name', Age: 'not-an-age'}";
            var formatter = new JsonInputFormatter();
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes);
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(User));
            var context = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            modelState.MaxAllowedErrors = 3;
            modelState.AddModelError("key1", "error1");
            modelState.AddModelError("key2", "error2");

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.True(result.HasError);
            Assert.False(modelState.ContainsKey("age"));
            var error = Assert.Single(modelState[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
        }

        [Fact]
        public void Creates_SerializerSettings_ByDefault()
        {
            // Arrange
            // Act
            var jsonFormatter = new JsonInputFormatter();

            // Assert
            Assert.NotNull(jsonFormatter.SerializerSettings);
        }

        [Fact]
        public void Constructor_UsesSerializerSettings()
        {
            // Arrange
            // Act
            var serializerSettings = new JsonSerializerSettings();
            var jsonFormatter = new JsonInputFormatter(serializerSettings);

            // Assert
            Assert.Same(serializerSettings, jsonFormatter.SerializerSettings);
        }

        [Fact]
        public async Task ChangesTo_DefaultSerializerSettings_TakesEffect()
        {
            // Arrange
            // missing password property here
            var contentBytes = Encoding.UTF8.GetBytes("{ \"UserName\" : \"John\"}");

            var jsonFormatter = new JsonInputFormatter();
            // by default we ignore missing members, so here explicitly changing it
            jsonFormatter.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, "application/json;charset=utf-8");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(UserLogin));
            var inputFormatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await jsonFormatter.ReadAsync(inputFormatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.False(modelState.IsValid);

            var modelErrorMessage = modelState.Values.First().Errors[0].Exception.Message;
            Assert.Contains("Required property 'Password' not found in JSON", modelErrorMessage);
        }

        [Fact]
        public async Task CustomSerializerSettingsObject_TakesEffect()
        {
            // Arrange
            // missing password property here
            var contentBytes = Encoding.UTF8.GetBytes("{ \"UserName\" : \"John\"}");

            var jsonFormatter = new JsonInputFormatter();
            // by default we ignore missing members, so here explicitly changing it
            jsonFormatter.SerializerSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            var modelState = new ModelStateDictionary();
            var httpContext = GetHttpContext(contentBytes, "application/json;charset=utf-8");
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(UserLogin));
            var inputFormatterContext = new InputFormatterContext(
                httpContext,
                modelName: string.Empty,
                modelState: modelState,
                metadata: metadata,
                readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

            // Act
            var result = await jsonFormatter.ReadAsync(inputFormatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.False(modelState.IsValid);

            var modelErrorMessage = modelState.Values.First().Errors[0].Exception.Message;
            Assert.Contains("Required property 'Password' not found in JSON", modelErrorMessage);
        }

        private static HttpContext GetHttpContext(
            byte[] contentBytes,
            string contentType = "application/json")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private IEnumerable<string> GetModelStateErrorMessages(ModelStateDictionary modelStateDictionary)
        {
            var allErrorMessages = new List<string>();
            foreach (var keyModelStatePair in modelStateDictionary)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    foreach (var modelError in errors)
                    {
                        if (string.IsNullOrEmpty(modelError.ErrorMessage))
                        {
                            if (modelError.Exception != null)
                            {
                                allErrorMessages.Add(modelError.Exception.Message);
                            }
                        }
                        else
                        {
                            allErrorMessages.Add(modelError.ErrorMessage);
                        }
                    }
                }
            }

            return allErrorMessages;
        }

        private sealed class User
        {
            public string Name { get; set; }

            public decimal Age { get; set; }

            public byte Small { get; set; }
        }

        private sealed class UserLogin
        {
            [JsonProperty(Required = Required.Always)]
            public string UserName { get; set; }

            [JsonProperty(Required = Required.Always)]
            public string Password { get; set; }
        }

        private class Location
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
#endif
