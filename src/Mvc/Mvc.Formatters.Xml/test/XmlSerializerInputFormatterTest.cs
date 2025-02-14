// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class XmlSerializerInputFormatterTest
{
    public class DummyClass
    {
        public int SampleInt { get; set; }
    }

    public class TestLevelOne
    {
        public int SampleInt { get; set; }
        public string sampleString;
        public DateTime SampleDate { get; set; }
    }

    public class TestLevelTwo
    {
        public string SampleString { get; set; }
        public TestLevelOne TestOne { get; set; }
    }

    [Fact]
    public async Task BuffersRequestBody_ByDefault()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString>" +
            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes, allowSyncReads: true);
        httpContext.Request.ContentType = "application/json";
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);
    }

    [Fact]
    public async Task SuppressInputFormatterBufferingSetToTrue_DoesNotBufferRequestBody_ObsoleteParameter()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString>" +
            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions { SuppressInputFormatterBuffering = true });

        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
        httpContext.Request.ContentType = "application/xml";
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);
    }

    [Fact]
    public async Task BuffersRequestBody_ByDefaultUsingMvcOptions()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString>" +
            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes, allowSyncReads: false);
        httpContext.Request.ContentType = "application/json";
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);
    }

    [Fact]
    public async Task SuppressInputFormatterBufferingSetToTrue_DoesNotBufferRequestBody()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString>" +
            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions() { SuppressInputFormatterBuffering = true });
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
        httpContext.Request.ContentType = "application/xml";
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);

        // Reading again should fail as buffering request body is disabled
        await Assert.ThrowsAsync<XmlException>(() => formatter.ReadAsync(context));
    }

    [Fact]
    public async Task SuppressInputFormatterBufferingSetToTrue_UsingMutatedOptions()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString>" +
            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var mvcOptions = new MvcOptions();
        mvcOptions.SuppressInputFormatterBuffering = false;
        var formatter = new XmlSerializerInputFormatter(mvcOptions);

        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
        httpContext.Request.Body = new NonSeekableReadStream(contentBytes);
        httpContext.Request.ContentType = "application/xml";
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        // Mutate options after passing into the constructor to make sure that the value type is not store in the constructor
        mvcOptions.SuppressInputFormatterBuffering = true;
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);

        // Reading again should fail as buffering request body is disabled
        await Assert.ThrowsAsync<XmlException>(() => formatter.ReadAsync(context));
    }

    [Theory]
    [InlineData("application/xml", true)]
    [InlineData("application/*", false)]
    [InlineData("*/*", false)]
    [InlineData("text/xml", true)]
    [InlineData("text/*", false)]
    [InlineData("text/json", false)]
    [InlineData("application/json", false)]
    [InlineData("application/some.entity+xml", true)]
    [InlineData("application/some.entity+xml;v=2", true)]
    [InlineData("application/some.entity+json", false)]
    [InlineData("application/some.entity+*", false)]
    [InlineData("text/some.entity+json", false)]
    [InlineData("", false)]
    [InlineData("invalid", false)]
    [InlineData(null, false)]
    public void CanRead_ReturnsTrueForAnySupportedContentType(string requestContentType, bool expectedCanRead)
    {
        // Arrange
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes("content");

        var modelState = new ModelStateDictionary();
        var httpContext = GetHttpContext(contentBytes, contentType: requestContentType);

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(string));
        var formatterContext = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: modelState,
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = formatter.CanRead(formatterContext);

        // Assert
        Assert.Equal(expectedCanRead, result);
    }

    [Theory]
    [InlineData(typeof(Dictionary<string, object>), false)]
    [InlineData(typeof(string), true)]
    public void CanRead_ReturnsFalse_ForAnyUnsupportedModelType(Type modelType, bool expectedCanRead)
    {
        // Arrange
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes("content");

        var context = GetInputFormatterContext(contentBytes, modelType);

        // Act
        var result = formatter.CanRead(context);

        // Assert
        Assert.Equal(expectedCanRead, result);
    }

    [Fact]
    public void XmlSerializer_CachesSerializerForType()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass><SampleInt>10</SampleInt></DummyClass>";
        var formatter = new TestXmlSerializerInputFormatter();
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

        // Act
        formatter.CanRead(context);
        formatter.CanRead(context);

        // Assert
        Assert.Equal(1, formatter.createSerializerCalledCount);
    }

    [Fact]
    public void HasProperSupportedMediaTypes()
    {
        // Arrange & Act
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        // Assert
        Assert.Contains("application/xml", formatter.SupportedMediaTypes
                             .Select(content => content.ToString()));
        Assert.Contains("text/xml", formatter.SupportedMediaTypes
                             .Select(content => content.ToString()));
    }

    [Fact]
    public void HasProperSupportedEncodings()
    {
        // Arrange & Act
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        // Assert
        Assert.Contains(formatter.SupportedEncodings, i => i.WebName == "utf-8");
        Assert.Contains(formatter.SupportedEncodings, i => i.WebName == "utf-16");
    }

    [Fact]
    public async Task ReadAsync_ReadsSimpleTypes()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
                            "<sampleString>" + expectedString + "</sampleString>" +
                            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.SampleDate);
    }

    [Fact]
    public async Task ReadAsync_ReadsComplexTypes()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);
        var expectedLevelTwoString = "102";

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<TestLevelTwo><SampleString>" + expectedLevelTwoString + "</SampleString>" +
                    "<TestOne><SampleInt>" + expectedInt + "</SampleInt>" +
                    "<sampleString>" + expectedString + "</sampleString>" +
                    "<SampleDate>" + expectedDateTime + "</SampleDate></TestOne></TestLevelTwo>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelTwo>(result.Model);

        Assert.Equal(expectedLevelTwoString, model.SampleString);
        Assert.Equal(expectedInt, model.TestOne.SampleInt);
        Assert.Equal(expectedString, model.TestOne.sampleString);
        Assert.Equal(
            XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc),
            model.TestOne.SampleDate);
    }

    [Fact]
    public async Task ReadAsync_ReadsWhenMaxDepthIsModified()
    {
        // Arrange
        var expectedInt = 10;

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass><SampleInt>" + expectedInt + "</SampleInt></DummyClass>";
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        formatter.MaxDepth = 10;
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<DummyClass>(result.Model);
        Assert.Equal(expectedInt, model.SampleInt);
    }

    [ConditionalFact]
    // ReaderQuotas are not honored on Mono
    [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
    public async Task ReadAsync_ThrowsOnExceededMaxDepth()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<TestLevelTwo><SampleString>test</SampleString>" +
                    "<TestOne><SampleInt>10</SampleInt>" +
                    "<sampleString>test</sampleString>" +
                    "<SampleDate>" + XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc)
                    + "</SampleDate></TestOne></TestLevelTwo>";
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        formatter.MaxDepth = 1;
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

        // Act & Assert
        await Assert.ThrowsAsync<InputFormatterException>(() => formatter.ReadAsync(context));
    }

    [ConditionalFact]
    // ReaderQuotas are not honored on Mono
    [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
    public async Task ReadAsync_ThrowsWhenReaderQuotasAreChanged()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<TestLevelTwo><SampleString>test</SampleString>" +
                    "<TestOne><SampleInt>10</SampleInt>" +
                    "<sampleString>test</sampleString>" +
                    "<SampleDate>" + XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc)
                    + "</SampleDate></TestOne></TestLevelTwo>";
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        formatter.XmlDictionaryReaderQuotas.MaxStringContentLength = 10;
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

        // Act & Assert
        await Assert.ThrowsAsync<InputFormatterException>(() => formatter.ReadAsync(context));
    }

    [Fact]
    public void SetMaxDepth_ThrowsWhenMaxDepthIsBelowOne()
    {
        // Arrange
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => formatter.MaxDepth = 0);
    }

    [Fact]
    public async Task ReadAsync_VerifyStreamIsOpenAfterRead()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass><SampleInt>10</SampleInt></DummyClass>";
        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.UTF8.GetBytes(input);
        var context = GetInputFormatterContext(contentBytes, typeof(DummyClass));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        Assert.NotNull(result.Model);
        Assert.True(context.HttpContext.Request.Body.CanRead);
    }

    [ReplaceCulture]
    [Fact]
    public async Task ReadAsync_FallsbackToUTF8_WhenCharSet_NotInContentType()
    {
        // Arrange
        var expectedException = typeof(XmlException);
        var expectedMessage = "The expected encoding 'utf-8' does not match the actual encoding 'utf-16LE'.";

        var inpStart = Encoding.Unicode.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-16\"?>" +
            "<DummyClass><SampleInt>");
        byte[] inp = { 192, 193 };
        var inpEnd = Encoding.Unicode.GetBytes("</SampleInt></DummyClass>");

        var contentBytes = new byte[inpStart.Length + inp.Length + inpEnd.Length];
        Buffer.BlockCopy(inpStart, 0, contentBytes, 0, inpStart.Length);
        Buffer.BlockCopy(inp, 0, contentBytes, inpStart.Length, inp.Length);
        Buffer.BlockCopy(inpEnd, 0, contentBytes, inpStart.Length + inp.Length, inpEnd.Length);

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

        // Act and Assert
        var ex = await Assert.ThrowsAsync(expectedException, () => formatter.ReadAsync(context));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    [ReplaceCulture]
    public async Task ReadAsync_UsesContentTypeCharSet_ToReadStream()
    {
        // Arrange
        var expectedException = typeof(XmlException);
        var expectedMessage = "The expected encoding 'utf-16LE' does not match the actual encoding 'utf-8'.";

        var inputBytes = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass><SampleInt>1000</SampleInt></DummyClass>");

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        var modelState = new ModelStateDictionary();
        var httpContext = GetHttpContext(inputBytes, contentType: "application/xml; charset=utf-16");

        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TestLevelOne));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: modelState,
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act and Assert
        var ex = await Assert.ThrowsAsync(expectedException, () => formatter.ReadAsync(context));
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public async Task ReadAsync_IgnoresBOMCharacters()
    {
        // Arrange
        var sampleString = "Test";
        var sampleStringBytes = Encoding.UTF8.GetBytes(sampleString);
        var inputStart = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
            "<TestLevelTwo><SampleString>" + sampleString);
        byte[] bom = { 0xef, 0xbb, 0xbf };
        var inputEnd = Encoding.UTF8.GetBytes("</SampleString></TestLevelTwo>");
        var expectedBytes = new byte[sampleString.Length + bom.Length];

        var contentBytes = new byte[inputStart.Length + bom.Length + inputEnd.Length];
        Buffer.BlockCopy(inputStart, 0, contentBytes, 0, inputStart.Length);
        Buffer.BlockCopy(bom, 0, contentBytes, inputStart.Length, bom.Length);
        Buffer.BlockCopy(inputEnd, 0, contentBytes, inputStart.Length + bom.Length, inputEnd.Length);

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var context = GetInputFormatterContext(contentBytes, typeof(TestLevelTwo));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelTwo>(result.Model);
        Buffer.BlockCopy(sampleStringBytes, 0, expectedBytes, 0, sampleStringBytes.Length);
        Buffer.BlockCopy(bom, 0, expectedBytes, sampleStringBytes.Length, bom.Length);
        Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(model.SampleString));
    }

    [Fact]
    public async Task ReadAsync_AcceptsUTF16Characters()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";
        var expectedDateTime = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

        var input = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>" +
                            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
                            "<sampleString>" + expectedString + "</sampleString>" +
                            "<SampleDate>" + expectedDateTime + "</SampleDate></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());
        var contentBytes = Encoding.Unicode.GetBytes(input);

        var modelState = new ModelStateDictionary();
        var httpContext = GetHttpContext(contentBytes, contentType: "application/xml; charset=utf-16");
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(TestLevelOne));
        var context = new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: modelState,
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.Equal(XmlConvert.ToDateTime(expectedDateTime, XmlDateTimeSerializationMode.Utc), model.SampleDate);
    }

    [Fact]
    public async Task ReadAsync_DoesNotDisposeBufferedStreamIfItDidNotCreateIt()
    {
        // Arrange
        var expectedInt = 10;
        var expectedString = "TestString";

        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<TestLevelOne><SampleInt>" + expectedInt + "</SampleInt>" +
            "<sampleString>" + expectedString + "</sampleString></TestLevelOne>";

        var formatter = new XmlSerializerInputFormatter(new MvcOptions());

        var contentBytes = Encoding.UTF8.GetBytes(input);
        var httpContext = new DefaultHttpContext();
        var testBufferedReadStream = new VerifyDisposeFileBufferingReadStream(new MemoryStream(contentBytes), 1024);
        httpContext.Request.Body = testBufferedReadStream;
        var context = GetInputFormatterContext(httpContext, typeof(TestLevelOne));

        // Act
        var result = await formatter.ReadAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasError);
        var model = Assert.IsType<TestLevelOne>(result.Model);

        Assert.Equal(expectedInt, model.SampleInt);
        Assert.Equal(expectedString, model.sampleString);
        Assert.False(testBufferedReadStream.Disposed);
    }

    private InputFormatterContext GetInputFormatterContext(byte[] contentBytes, Type modelType)
    {
        var httpContext = GetHttpContext(contentBytes);
        return GetInputFormatterContext(httpContext, modelType);
    }

    private InputFormatterContext GetInputFormatterContext(HttpContext httpContext, Type modelType)
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(modelType);
        return new InputFormatterContext(
            httpContext,
            modelName: string.Empty,
            modelState: new ModelStateDictionary(),
            metadata: metadata,
            readerFactory: new TestHttpRequestStreamReaderFactory().CreateReader);
    }

    private static HttpContext GetHttpContext(
        byte[] contentBytes,
        string contentType = "application/xml")
    {
        var request = new Mock<HttpRequest>();
        var headers = new Mock<IHeaderDictionary>();
        request.SetupGet(r => r.Headers).Returns(headers.Object);
        request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
        request.SetupGet(f => f.ContentType).Returns(contentType);
        request.SetupGet(f => f.ContentLength).Returns(contentBytes.Length);

        var httpContext = new Mock<HttpContext>();
        var features = new Mock<IFeatureCollection>();
        httpContext.SetupGet(c => c.Request).Returns(request.Object);
        httpContext.SetupGet(c => c.Features).Returns(features.Object);
        return httpContext.Object;
    }

    private class TestXmlSerializerInputFormatter : XmlSerializerInputFormatter
    {
        public int createSerializerCalledCount = 0;

        public TestXmlSerializerInputFormatter()
            : base(new MvcOptions())
        {
        }

        protected override XmlSerializer CreateSerializer(Type type)
        {
            createSerializerCalledCount++;
            return base.CreateSerializer(type);
        }
    }

    private class TestResponseFeature : HttpResponseFeature
    {
        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            // do not do anything
        }
    }

    private class VerifyDisposeFileBufferingReadStream : FileBufferingReadStream
    {
        public bool Disposed { get; private set; }
        public VerifyDisposeFileBufferingReadStream(Stream inner, int memoryThreshold) : base(inner, memoryThreshold)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return base.DisposeAsync();
        }
    }
}
