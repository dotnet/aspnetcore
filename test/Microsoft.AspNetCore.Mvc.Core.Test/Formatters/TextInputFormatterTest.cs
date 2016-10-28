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
    public class TextInputFormatterTest
    {
        [Fact]
        public async Task ReadAsync_ReturnsFailure_IfItCanNotUnderstandTheContentTypeEncoding()
        {
            // Arrange
            var formatter = new TestFormatter();
            formatter.SupportedEncodings.Add(Encoding.ASCII);

            var context = new InputFormatterContext(
                new DefaultHttpContext(),
                "something",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
                (stream, encoding) => new StreamReader(stream, encoding));

            context.HttpContext.Request.ContentType = "application/json;charset=utf-8";
            context.HttpContext.Request.ContentLength = 1;

            // Act
            var result = await formatter.ReadAsync(context);

            // Assert
            Assert.Equal(true, result.HasError);
            Assert.Equal(true, context.ModelState.ContainsKey("something"));
            Assert.Equal(1, context.ModelState["something"].Errors.Count);

            var error = context.ModelState["something"].Errors[0];
            Assert.IsType<UnsupportedContentTypeException>(error.Exception);
        }

        [Fact]
        public void SelectCharacterEncoding_ThrowsInvalidOperationException_IfItDoesNotHaveAValidEncoding()
        {
            // Arrange
            var formatter = new TestFormatter();

            var context = new InputFormatterContext(
                new DefaultHttpContext(),
                "something",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
                (stream, encoding) => new StreamReader(stream, encoding));

            context.HttpContext.Request.ContentLength = 1;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => formatter.TestSelectCharacterEncoding(context));
        }

        [Fact]
        public void SelectCharacterEncoding_ReturnsNull_IfItCanNotUnderstandContentTypeEncoding()
        {
            // Arrange
            var formatter = new TestFormatter();
            formatter.SupportedEncodings.Add(Encoding.UTF32);

            var context = new InputFormatterContext(
                new DefaultHttpContext(),
                "something",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
                (stream, encoding) => new StreamReader(stream, encoding));

            context.HttpContext.Request.ContentType = "application/json;charset=utf-8";

            // Act
            var result = formatter.TestSelectCharacterEncoding(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SelectCharacterEncoding_ReturnsContentTypeEncoding_IfItCanUnderstandIt()
        {
            // Arrange
            var formatter = new TestFormatter();
            formatter.SupportedEncodings.Add(Encoding.UTF32);
            formatter.SupportedEncodings.Add(Encoding.UTF8);

            var context = new InputFormatterContext(
                new DefaultHttpContext(),
                "something",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
                (stream, encoding) => new StreamReader(stream, encoding));

            context.HttpContext.Request.ContentType = "application/json;charset=utf-8";

            // Act
            var result = formatter.TestSelectCharacterEncoding(context);

            // Assert
            Assert.Equal(Encoding.UTF8, result);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("")]
        public void SelectCharacterEncoding_ReturnsFirstEncoding_IfContentTypeIsNotSpecifiedOrDoesNotHaveEncoding(string contentType)
        {
            // Arrange
            var formatter = new TestFormatter();
            formatter.SupportedEncodings.Add(Encoding.UTF8);
            formatter.SupportedEncodings.Add(Encoding.UTF32);

            var context = new InputFormatterContext(
                new DefaultHttpContext(),
                "something",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
                (stream, encoding) => new StreamReader(stream, encoding));

            context.HttpContext.Request.ContentType = contentType;

            // Act
            var result = formatter.TestSelectCharacterEncoding(context);

            // Assert
            Assert.Equal(Encoding.UTF8, result);
        }

        private class TestFormatter : TextInputFormatter
        {
            private readonly object _object;

            public TestFormatter() : this(null) { }

            public TestFormatter(object @object)
            {
                _object = @object;
            }

            public IList<Type> SupportedTypes { get; } = new List<Type>();

            protected override bool CanReadType(Type type)
            {
                return SupportedTypes.Count == 0 ? true : SupportedTypes.Contains(type);
            }

            public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
            {
                return InputFormatterResult.SuccessAsync(_object);
            }

            public Encoding TestSelectCharacterEncoding(InputFormatterContext context)
            {
                return SelectCharacterEncoding(context);
            }
        }
    }
}
