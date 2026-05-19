// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Formatters;

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
        Assert.True(result.HasError);
        Assert.True(context.ModelState.ContainsKey("something"));
        Assert.Single(context.ModelState["something"].Errors);

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

    [Theory]
    [InlineData("utf-8")]
    [InlineData("invalid")]
    public void SelectCharacterEncoding_ReturnsNull_IfItCanNotUnderstandContentTypeEncoding(string charset)
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

        context.HttpContext.Request.ContentType = "application/json;charset=" + charset;

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
    [InlineData("unicode-1-1-utf-8")]
    [InlineData("unicode-2-0-utf-8")]
    public void SelectCharacterEncoding_ReturnsUTF8Encoding_IfContentTypeIsAnAlias(string charset)
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

        context.HttpContext.Request.ContentType = "application/json;charset=" + charset;

        // Act
        var result = formatter.TestSelectCharacterEncoding(context);

        // Assert
        Assert.Equal(Encoding.UTF8, result);
    }

    [Theory]
    [InlineData("ANSI_X3.4-1968")]
    [InlineData("ANSI_X3.4-1986")]
    [InlineData("ascii")]
    [InlineData("cp367")]
    [InlineData("csASCII")]
    [InlineData("IBM367")]
    [InlineData("iso-ir-6")]
    [InlineData("ISO646-US")]
    [InlineData("ISO_646.irv:1991")]
    [InlineData("us")]
    public void SelectCharacterEncoding_ReturnsAsciiEncoding_IfContentTypeIsAnAlias(string charset)
    {
        // Arrange
        var formatter = new TestFormatter();
        formatter.SupportedEncodings.Add(Encoding.UTF32);
        formatter.SupportedEncodings.Add(Encoding.ASCII);

        var context = new InputFormatterContext(
            new DefaultHttpContext(),
            "something",
            new ModelStateDictionary(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(object)),
            (stream, encoding) => new StreamReader(stream, encoding));

        context.HttpContext.Request.ContentType = "application/json;charset=\"" + charset + "\"";

        // Act
        var result = formatter.TestSelectCharacterEncoding(context);

        // Assert
        Assert.Equal(Encoding.ASCII, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("(garbage)")]
    [InlineData("(garbage); charset=utf-32")]
    [InlineData("text/(garbage)")]
    [InlineData("text/(garbage); charset=utf-32")]
    [InlineData("application/json")]
    [InlineData("application/json; charset")]
    [InlineData("application/json; charset=(garbage)")]
    [InlineData("application/json; version=(garbage); charset=utf-32")]
    public void SelectCharacterEncoding_ReturnsFirstEncoding_IfContentTypeIsMissingInvalidOrDoesNotHaveEncoding(
        string contentType)
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
