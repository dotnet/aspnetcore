// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class XmlSerializerOutputFormatterTest
{
    public class DummyClass
    {
        public int SampleInt { get; set; }
    }

    public class TestLevelOne
    {
        public int SampleInt { get; set; }
        public string sampleString;
    }

    public class TestLevelTwo
    {
        public string SampleString { get; set; }
        public TestLevelOne TestOne { get; set; }
    }

    public static IEnumerable<object[]> BasicTypeValues
    {
        get
        {
            yield return new object[] { "sampleString", "<string>sampleString</string>" };
            yield return new object[] { 5, "<int>5</int>" };
            yield return new object[] { 5.43, "<double>5.43</double>" };
            yield return new object[] { 'a', "<char>97</char>" };
            yield return new object[] { new DummyClass { SampleInt = 10 }, "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                    "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>" };
        }
    }

    [Theory]
    [MemberData(nameof(BasicTypeValues))]
    public async Task XmlSerializerOutputFormatterCanWriteBasicTypes(object input, string expectedOutput)
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(input, input.GetType());

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    public static TheoryData<bool, object, string> CanIndentOutputConditionallyData
    {
        get
        {
            var obj = new DummyClass { SampleInt = 10 };
            var newLine = Environment.NewLine;
            return new TheoryData<bool, object, string>()
                {
                    { true, obj, "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                        $"xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">{newLine}  <SampleInt>10</SampleInt>{newLine}</DummyClass>" },
                    { false, obj, "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                        "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>" }
                };
        }
    }

    [Theory]
    [MemberData(nameof(CanIndentOutputConditionallyData))]
    public async Task XmlSerializer_CanIndentOutputConditionally(bool indent, object input, string expectedOutput)
    {
        // Arrange
        var formatter = new IndentingXmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(input, input.GetType());
        outputFormatterContext.HttpContext.Request.QueryString = new QueryString("?indent=" + indent);

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        Assert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializer_CanModifyNamespacesInGeneratedXml()
    {
        // Arrange
        var input = new DummyClass { SampleInt = 10 };
        var formatter = new IgnoreAmbientNamespacesXmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(input, input.GetType());
        var expectedOutput = "<DummyClass><SampleInt>10</SampleInt></DummyClass>";

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        Assert.Equal(expectedOutput, content);
    }

    [Fact]
    public void XmlSerializer_CachesSerializerForType()
    {
        // Arrange
        var input = new DummyClass { SampleInt = 10 };
        var formatter = new TestXmlSerializerOutputFormatter();

        var context = GetOutputFormatterContext(input, typeof(DummyClass));
        context.ContentType = new StringSegment("application/xml");

        // Act
        formatter.CanWriteResult(context);
        formatter.CanWriteResult(context);

        // Assert
        Assert.Equal(1, formatter.createSerializerCalledCount);
    }

    [Fact]
    public void DefaultConstructor_ExpectedWriterSettings_Created()
    {
        // Arrange and Act
        var formatter = new XmlSerializerOutputFormatter();

        // Assert
        var writerSettings = formatter.WriterSettings;
        Assert.NotNull(writerSettings);
        Assert.True(writerSettings.OmitXmlDeclaration);
        Assert.False(writerSettings.CloseOutput);
        Assert.False(writerSettings.CheckCharacters);
    }

    [Fact]
    public async Task SuppliedWriterSettings_TakeAffect()
    {
        // Arrange
        var writerSettings = FormattingUtilities.GetDefaultXmlWriterSettings();
        writerSettings.OmitXmlDeclaration = false;
        var sampleInput = new DummyClass { SampleInt = 10 };
        var formatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
        var formatter = new XmlSerializerOutputFormatter(writerSettings);
        var expectedOutput = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

        // Act
        await formatter.WriteAsync(formatterContext);

        // Assert
        var body = formatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesSimpleTypes()
    {
        // Arrange
        var expectedOutput =
            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

        var sampleInput = new DummyClass { SampleInt = 10 };
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesComplexTypes()
    {
        // Arrange
        var expectedOutput =
            "<TestLevelTwo xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleString>TestString</SampleString>" +
            "<TestOne><sampleString>TestLevelOne string</sampleString>" +
            "<SampleInt>10</SampleInt></TestOne></TestLevelTwo>";

        var sampleInput = new TestLevelTwo
        {
            SampleString = "TestString",
            TestOne = new TestLevelOne
            {
                SampleInt = 10,
                sampleString = "TestLevelOne string"
            }
        };
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesOnModifiedWriterSettings()
    {
        // Arrange
        var expectedOutput =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

        var sampleInput = new DummyClass { SampleInt = 10 };
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());
        var formatter = new XmlSerializerOutputFormatter(
            new System.Xml.XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                CloseOutput = false
            });

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesUTF16Output()
    {
        // Arrange
        var expectedOutput =
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>";

        var sampleInput = new DummyClass { SampleInt = 10 };
        var outputFormatterContext =
            GetOutputFormatterContext(sampleInput, sampleInput.GetType(), "application/xml; charset=utf-16");
        var formatter = new XmlSerializerOutputFormatter();
        formatter.WriterSettings.OmitXmlDeclaration = false;

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;
        var content = new StreamReader(
            body,
            new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesIndentedOutput()
    {
        // Arrange
        var expectedOutput =
            "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
            "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <SampleInt>10</SampleInt>\r\n</DummyClass>";

        var sampleInput = new DummyClass { SampleInt = 10 };
        var formatter = new XmlSerializerOutputFormatter();
        formatter.WriterSettings.Indent = true;
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        var body = outputFormatterContext.HttpContext.Response.Body;
        body.Position = 0;

        var content = new StreamReader(body).ReadToEnd();
        XmlAssert.Equal(expectedOutput, content);
    }

    [Fact]
    public async Task VerifyBodyIsNotClosedAfterOutputIsWritten()
    {
        // Arrange
        var sampleInput = new DummyClass { SampleInt = 10 };
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

        // Act
        await formatter.WriteAsync(outputFormatterContext);

        // Assert
        Assert.NotNull(outputFormatterContext.HttpContext.Response.Body);
        Assert.True(outputFormatterContext.HttpContext.Response.Body.CanRead);
    }

    public static IEnumerable<object[]> TypesForCanWriteResult
    {
        get
        {
            yield return new object[] { null, typeof(string), true };
            yield return new object[] { null, null, false };
            yield return new object[] { new DummyClass { SampleInt = 5 }, typeof(DummyClass), true };
            yield return new object[] { null, typeof(object), true };
            yield return new object[] {
                    new Dictionary<string, string> { { "Hello", "world" } }, typeof(Dictionary<string,string>), false };
            yield return new object[] {
                    new[] {"value1", "value2"}, typeof(IEnumerable<string>), true };
            yield return new object[] {
                    Enumerable.Range(1, 2).Select(i => "value" + i).AsQueryable(), typeof(IQueryable<string>), true };
        }
    }

    [Theory]
    [MemberData(nameof(TypesForCanWriteResult))]
    public void CanWriteResult_ReturnsExpectedValueForObjectType(object input, Type declaredType, bool expectedOutput)
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(input, declaredType);
        outputFormatterContext.ContentType = new StringSegment("application/xml");

        // Act
        var result = formatter.CanWriteResult(outputFormatterContext);

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    [Theory]
    [InlineData("application/xml", false, "application/xml")]
    [InlineData("application/xml", true, "application/xml")]
    [InlineData("application/other", false, null)]
    [InlineData("application/other", true, null)]
    [InlineData("application/*", false, "application/xml")]
    [InlineData("text/*", false, "text/xml")]
    [InlineData("custom/*", false, null)]
    [InlineData("application/xml;v=2", false, null)]
    [InlineData("application/xml;v=2", true, null)]
    [InlineData("application/some.entity+xml", false, null)]
    [InlineData("application/some.entity+xml", true, "application/some.entity+xml")]
    [InlineData("application/some.entity+xml;v=2", true, "application/some.entity+xml;v=2")]
    [InlineData("application/some.entity+other", true, null)]
    public void CanWriteResult_ReturnsExpectedValueForMediaType(
        string mediaType,
        bool isServerDefined,
        string expectedResult)
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(new object(), typeof(object));
        outputFormatterContext.ContentType = new StringSegment(mediaType);
        outputFormatterContext.ContentTypeIsServerDefined = isServerDefined;

        // Act
        var actualCanWriteValue = formatter.CanWriteResult(outputFormatterContext);

        // Assert
        var expectedContentType = expectedResult ?? mediaType;
        Assert.Equal(expectedResult != null, actualCanWriteValue);
        Assert.Equal(new StringSegment(expectedContentType), outputFormatterContext.ContentType);
    }

    [Fact]
    public async Task XmlSerializerOutputFormatterWritesContentLengthResponse()
    {
        // Arrange
        var sampleInput = new DummyClass { SampleInt = 10 };
        var formatter = new XmlSerializerOutputFormatter();
        var outputFormatterContext = GetOutputFormatterContext(sampleInput, sampleInput.GetType());

        var response = outputFormatterContext.HttpContext.Response;
        response.Body = Stream.Null;

        // Act & Assert
        await formatter.WriteAsync(outputFormatterContext);

        Assert.NotNull(outputFormatterContext.HttpContext.Response.ContentLength);
    }

    public static IEnumerable<object[]> TypesForGetSupportedContentTypes
    {
        get
        {
            yield return new object[] { typeof(DummyClass), "application/xml" };
            yield return new object[] { typeof(object), "application/xml" };
            yield return new object[] { null, null };
        }
    }

    [Theory]
    [MemberData(nameof(TypesForGetSupportedContentTypes))]
    public void XmlSerializer_GetSupportedContentTypes_Returns_SupportedTypes(Type type, object expectedOutput)
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();

        // Act
        var result = formatter.GetSupportedContentTypes("application/xml", type);

        // Assert
        if (expectedOutput != null)
        {
            Assert.Equal(expectedOutput, Assert.Single(result).ToString());
        }
        else
        {
            Assert.Equal(expectedOutput, result);
        }
    }

    public static TheoryData<XmlSerializerOutputFormatter, TestSink> LogsWhenUnableToCreateSerializerForTypeData
    {
        get
        {
            var sink1 = new TestSink();
            var formatter1 = new XmlSerializerOutputFormatter(new TestLoggerFactory(sink1, enabled: true));

            var sink2 = new TestSink();
            var formatter2 = new XmlSerializerOutputFormatter(
                new XmlWriterSettings(),
                new TestLoggerFactory(sink2, enabled: true));

            return new TheoryData<XmlSerializerOutputFormatter, TestSink>()
                {
                    { formatter1, sink1 },
                    { formatter2, sink2}
                };
        }
    }

    [Theory]
    [MemberData(nameof(LogsWhenUnableToCreateSerializerForTypeData))]
    public void XmlSerializer_LogsWhenUnableToCreateSerializerForType(
        XmlSerializerOutputFormatter formatter,
        TestSink sink)
    {
        // Arrange
        var outputFormatterContext = GetOutputFormatterContext(new Customer(10), typeof(Customer));

        // Act
        var canWriteResult = formatter.CanWriteResult(outputFormatterContext);

        // Assert
        Assert.False(canWriteResult);
        var write = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal(
            $"An error occurred while trying to create an XmlSerializer for the type '{typeof(Customer).FullName}'.",
            write.State.ToString());
    }

    [Fact]
    public void XmlSerializer_DoesNotThrow_OnNoLoggerAnd_WhenUnableToCreateSerializerForType()
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter(); // no logger is being supplied here on purpose
        var outputFormatterContext = GetOutputFormatterContext(new Customer(10), typeof(Customer));

        // Act
        var canWriteResult = formatter.CanWriteResult(outputFormatterContext);

        // Assert
        Assert.False(canWriteResult);
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AsyncEnumerableConnectionCloses()
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();
        var body = new MemoryStream();
        var cts = new CancellationTokenSource();
        var iterated = false;

        var asyncEnumerable = AsyncEnumerableClosedConnection();
        var outputFormatterContext = GetOutputFormatterContext(
            asyncEnumerable,
            asyncEnumerable.GetType());
        outputFormatterContext.HttpContext.RequestAborted = cts.Token;
        outputFormatterContext.HttpContext.Response.Body = body;

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        Assert.Empty(body.ToArray());
        Assert.False(iterated);

        async IAsyncEnumerable<int> AsyncEnumerableClosedConnection([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cts.Cancel();
            // MvcOptions.MaxIAsyncEnumerableBufferLimit is 8192. Pick some value larger than that.
            foreach (var i in Enumerable.Range(0, 9000))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                iterated = true;
                yield return i;
            }
        }
    }

    [Fact]
    public async Task WriteResponseBodyAsync_AsyncEnumerable()
    {
        // Arrange
        var formatter = new XmlSerializerOutputFormatter();
        var body = new MemoryStream();

        var asyncEnumerable = AsyncEnumerable();
        var outputFormatterContext = GetOutputFormatterContext(
            asyncEnumerable,
            asyncEnumerable.GetType());
        outputFormatterContext.HttpContext.Response.Body = body;

        // Act
        await formatter.WriteResponseBodyAsync(outputFormatterContext, Encoding.GetEncoding("utf-8"));

        // Assert
        Assert.Contains("<int>1</int><int>2</int>", Encoding.UTF8.GetString(body.ToArray()));

        async IAsyncEnumerable<int> AsyncEnumerable()
        {
            await Task.Yield();
            yield return 1;
            yield return 2;
        }
    }

    private OutputFormatterWriteContext GetOutputFormatterContext(
        object outputValue,
        Type outputType,
        string contentType = "application/xml; charset=utf-8")
    {
        return new OutputFormatterWriteContext(
            GetHttpContext(contentType),
            new TestHttpResponseStreamWriterFactory().CreateWriter,
            outputType,
            outputValue);
    }

    private static HttpContext GetHttpContext(string contentType)
    {
        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        request.Headers["Accept-Charset"] = MediaTypeHeaderValue.Parse(contentType).Charset.ToString();
        request.ContentType = contentType;
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(Options.Create(new MvcOptions()))
            .BuildServiceProvider();
        return httpContext;
    }

    private class TestXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
    {
        public int createSerializerCalledCount = 0;

        protected override XmlSerializer CreateSerializer(Type type)
        {
            createSerializerCalledCount++;
            return base.CreateSerializer(type);
        }
    }

    public class Customer
    {
        public Customer(int id)
        {
        }

        public int MyProperty { get; set; }
    }
    private class IndentingXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
    {
        public override XmlWriter CreateXmlWriter(
            OutputFormatterWriteContext context,
            TextWriter writer,
            XmlWriterSettings xmlWriterSettings)
        {
            var request = context.HttpContext.Request;
            if (request.Query["indent"] == "True")
            {
                xmlWriterSettings.Indent = true;
            }

            return base.CreateXmlWriter(context, writer, xmlWriterSettings);
        }
    }

    private class IgnoreAmbientNamespacesXmlSerializerOutputFormatter : XmlSerializerOutputFormatter
    {
        protected override void Serialize(XmlSerializer xmlSerializer, XmlWriter xmlWriter, object value)
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            xmlSerializer.Serialize(xmlWriter, value, namespaces);
        }
    }
}
