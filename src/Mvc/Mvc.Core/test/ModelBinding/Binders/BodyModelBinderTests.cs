// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Net.Http.Headers;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class BodyModelBinderTests
{
    [Fact]
    public async Task BindModel_CallsSelectedInputFormatterOnce()
    {
        // Arrange
        var mockInputFormatter = new Mock<IInputFormatter>();
        mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
            .Returns(true)
            .Verifiable();
        mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                          .Returns(InputFormatterResult.SuccessAsync(new Person()))
                          .Verifiable();
        var inputFormatter = mockInputFormatter.Object;

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(
            typeof(Person),
            metadataProvider: provider);

        var binder = CreateBinder(new[] { inputFormatter });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        mockInputFormatter.Verify(v => v.CanRead(It.IsAny<InputFormatterContext>()), Times.Once);
        mockInputFormatter.Verify(v => v.ReadAsync(It.IsAny<InputFormatterContext>()), Times.Once);
        Assert.True(bindingContext.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModel_NoInputFormatterFound_SetsModelStateError()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

        var binder = CreateBinder(new List<IInputFormatter>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        Assert.Single(entry.Value.Errors);
    }

    [Fact]
    public async Task BindModel_NoInputFormatterFound_SetsModelStateError_RespectsBinderModelName()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
        bindingContext.BinderModelName = "custom";

        var binder = CreateBinder(new List<IInputFormatter>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the bindermodelname because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal("custom", entry.Key);
        Assert.Single(entry.Value.Errors);
    }

    [Fact]
    public async Task BindModel_IsGreedy()
    {
        // Arrange
        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);

        var binder = CreateBinder(new List<IInputFormatter>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
    }

    [Fact]
    public async Task BindModel_NoValueResult_SetsModelStateError()
    {
        // Arrange
        var mockInputFormatter = new Mock<IInputFormatter>();
        mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
            .Returns(true);
        mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
            .Returns(InputFormatterResult.NoValueAsync());
        var inputFormatter = mockInputFormatter.Object;

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d =>
        {
            d.BindingSource = BindingSource.Body;
            d.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(
                () => "Customized error message");
        });

        var bindingContext = GetBindingContext(
            typeof(Person),
            metadataProvider: provider);
        bindingContext.BinderModelName = "custom";

        var binder = CreateBinder(new[] { inputFormatter });

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Null(bindingContext.Result.Model);
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.False(bindingContext.ModelState.IsValid);

        // Key is the bindermodelname because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal("custom", entry.Key);
        Assert.Equal("Customized error message", entry.Value.Errors.Single().ErrorMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BindModel_PassesAllowEmptyInputOptionViaContext(bool treatEmptyInputAsDefaultValueOption)
    {
        // Arrange
        var mockInputFormatter = new Mock<IInputFormatter>();
        mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
            .Returns(true);
        mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
            .Returns(InputFormatterResult.NoValueAsync())
            .Verifiable();
        var inputFormatter = mockInputFormatter.Object;

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(
            typeof(Person),
            metadataProvider: provider);
        bindingContext.BinderModelName = "custom";

        var binder = CreateBinder(new[] { inputFormatter }, treatEmptyInputAsDefaultValueOption);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        mockInputFormatter.Verify(formatter => formatter.ReadAsync(
            It.Is<InputFormatterContext>(ctx => ctx.TreatEmptyInputAsDefaultValue == treatEmptyInputAsDefaultValueOption)),
            Times.Once);
    }

    [Fact]
    public async Task BindModel_SetsModelIfAllowEmpty()
    {
        // Arrange
        var mockInputFormatter = new Mock<IInputFormatter>();
        mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
            .Returns(false);
        var inputFormatter = mockInputFormatter.Object;

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(
            typeof(Person),
            metadataProvider: provider);
        bindingContext.BinderModelName = "custom";

        var binder = CreateBinder(new[] { inputFormatter }, treatEmptyInputAsDefaultValueOption: true);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);
        Assert.True(bindingContext.ModelState.IsValid);
    }

    [Fact]
    public async Task BindModel_FailsIfNotAllowEmpty()
    {
        // Arrange
        var mockInputFormatter = new Mock<IInputFormatter>();
        mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
            .Returns(false);
        var inputFormatter = mockInputFormatter.Object;

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(
            typeof(Person),
            metadataProvider: provider);
        bindingContext.BinderModelName = "custom";

        var binder = CreateBinder(new[] { inputFormatter }, treatEmptyInputAsDefaultValueOption: false);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.ModelState.IsValid);
        Assert.Single(bindingContext.ModelState[bindingContext.BinderModelName].Errors);
        Assert.Equal("Unsupported content type ''.", bindingContext.ModelState[bindingContext.BinderModelName].Errors[0].Exception.Message);
    }

    // Throwing InputFormatterException
    [Fact]
    public async Task BindModel_CustomFormatter_ThrowingInputFormatterException_AddsErrorToModelState()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
        httpContext.Request.ContentType = "text/xyz";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var expectedFormatException = new FormatException("bad format!");
        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var formatter = new XyzFormatter((inputFormatterContext, encoding) =>
        {
            throw new InputFormatterException("Bad input!!", expectedFormatException);
        });
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        var errorMessage = Assert.Single(entry.Value.Errors).ErrorMessage;
        Assert.Equal("Bad input!!", errorMessage);
        Assert.Null(entry.Value.Errors[0].Exception);
    }

    public static TheoryData<IInputFormatter> BuiltInFormattersThrowingInputFormatterException
    {
        get
        {
            return new TheoryData<IInputFormatter>()
                {
                    { new XmlSerializerInputFormatter(new MvcOptions()) },
                    { new XmlDataContractSerializerInputFormatter(new MvcOptions()) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(BuiltInFormattersThrowingInputFormatterException))]
    public async Task BindModel_BuiltInXmlInputFormatters_ThrowingInputFormatterException_AddsErrorToModelState(
        IInputFormatter formatter)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
        httpContext.Request.ContentType = "application/xml";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        var errorMessage = Assert.Single(entry.Value.Errors).ErrorMessage;
        Assert.Equal("An error occurred while deserializing input data.", errorMessage);
        Assert.Null(entry.Value.Errors[0].Exception);
    }

    [Fact]
    public async Task BindModel_BuiltInJsonInputFormatter_ThrowingInputFormatterException_AddsErrorToModelState()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
        httpContext.Request.ContentType = "application/json";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(
            new[] { new TestableJsonInputFormatter(throwNonInputFormatterException: false) },
            new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        Assert.NotEmpty(entry.Value.Errors[0].ErrorMessage);
    }

    public static TheoryData<IInputFormatter> DerivedFormattersThrowingInputFormatterException
    {
        get
        {
            return new TheoryData<IInputFormatter>()
                {
                    { new DerivedXmlSerializerInputFormatter(throwNonInputFormatterException: false) },
                    { new DerivedXmlDataContractSerializerInputFormatter(throwNonInputFormatterException: false) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DerivedFormattersThrowingInputFormatterException))]
    public async Task BindModel_DerivedXmlInputFormatters_AddsErrorToModelState(IInputFormatter formatter)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
        httpContext.Request.ContentType = "application/xml";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        var errorMessage = Assert.Single(entry.Value.Errors).ErrorMessage;
        Assert.Equal("An error occurred while deserializing input data.", errorMessage);
        Assert.Null(entry.Value.Errors[0].Exception);
    }

    [Fact]
    public async Task BindModel_DerivedJsonInputFormatter_AddsErrorToModelState()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Bad data!"));
        httpContext.Request.ContentType = "application/json";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(
            new[] { new DerivedJsonInputFormatter(throwNonInputFormatterException: false) },
            new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        Assert.NotEmpty(entry.Value.Errors[0].ErrorMessage);
        Assert.Null(entry.Value.Errors[0].Exception);
    }

    // Throwing Non-InputFormatterException
    public static TheoryData<IInputFormatter, string> BuiltInFormattersThrowingNonInputFormatterException
    {
        get
        {
            return new TheoryData<IInputFormatter, string>()
                {
                    { new TestableXmlSerializerInputFormatter(throwNonInputFormatterException: true), "text/xml" },
                    { new TestableXmlDataContractSerializerInputFormatter(throwNonInputFormatterException: true), "text/xml" },
                    { new TestableJsonInputFormatter(throwNonInputFormatterException: true), "text/json" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(BuiltInFormattersThrowingNonInputFormatterException))]
    public async Task BindModel_BuiltInInputFormatters_ThrowingNonInputFormatterException_Throws(
        IInputFormatter formatter,
        string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("valid data!"));
        httpContext.Request.ContentType = contentType;

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IOException>(() => binder.BindModelAsync(bindingContext));
        Assert.Equal("Unable to read input stream!!", exception.Message);
    }

    public static TheoryData<IInputFormatter, string> DerivedInputFormattersThrowingNonInputFormatterException
    {
        get
        {
            return new TheoryData<IInputFormatter, string>()
                {
                    { new DerivedXmlSerializerInputFormatter(throwNonInputFormatterException: true), "text/xml" },
                    { new DerivedXmlDataContractSerializerInputFormatter(throwNonInputFormatterException: true), "text/xml" },
                    { new DerivedJsonInputFormatter(throwNonInputFormatterException: true), "text/json" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DerivedInputFormattersThrowingNonInputFormatterException))]
    public async Task BindModel_DerivedXmlInputFormatters_ThrowingNonInputFormattingException_AddsErrorToModelState(
        IInputFormatter formatter,
        string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("valid data!"));
        httpContext.Request.ContentType = contentType;

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        var errorMessage = Assert.Single(entry.Value.Errors).Exception.Message;
        Assert.Equal("Unable to read input stream!!", errorMessage);
        Assert.IsType<IOException>(entry.Value.Errors[0].Exception);
    }

    [Fact]
    public async Task BindModel_CustomFormatter_ThrowingNonInputFormatterException_Throws()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("valid data"));
        httpContext.Request.ContentType = "text/xyz";

        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(typeof(Person), httpContext, metadataProvider);
        var formatter = new XyzFormatter((inputFormatterContext, encoding) =>
        {
            throw new IOException("Unable to read input stream!!");
        });
        var binder = CreateBinder(new[] { formatter }, new MvcOptions());

        // Act
        var exception = await Assert.ThrowsAsync<IOException>(
            () => binder.BindModelAsync(bindingContext));
        Assert.Equal("Unable to read input stream!!", exception.Message);
    }

    [Fact]
    public async Task NullFormatterError_AddedToModelState()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "text/xyz";

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);

        var bindingContext = GetBindingContext(
            typeof(Person),
            httpContext: httpContext,
            metadataProvider: provider);

        var binder = CreateBinder(new List<IInputFormatter>());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
        Assert.Null(bindingContext.Result.Model);

        // Key is the empty string because this was a top-level binding.
        var entry = Assert.Single(bindingContext.ModelState);
        Assert.Equal(string.Empty, entry.Key);
        var errorMessage = Assert.Single(entry.Value.Errors).Exception.Message;
        Assert.Equal("Unsupported content type 'text/xyz'.", errorMessage);
    }

    [Fact]
    public async Task BindModelCoreAsync_UsesFirstFormatterWhichCanRead()
    {
        // Arrange
        var canReadFormatter1 = new TestInputFormatter(canRead: true);
        var canReadFormatter2 = new TestInputFormatter(canRead: true);
        var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: false),
                canReadFormatter1,
                canReadFormatter2
            };

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
        var binder = CreateBinder(inputFormatters);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Same(canReadFormatter1, bindingContext.Result.Model);
    }

    [Fact]
    public async Task BindModelAsync_LogsFormatterRejectionAndSelection()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: true),
            };

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
        bindingContext.HttpContext.Request.ContentType = "application/json";
        var binder = new BodyModelBinder(inputFormatters, new TestHttpRequestStreamReaderFactory(), loggerFactory);

        // Act
        await binder.BindModelAsync(bindingContext);

        var writeList = sink.Writes.ToList();

        // Assert
        Assert.Equal($"Attempting to bind model of type '{typeof(Person)}' using the name 'someName' in request data ...", writeList[0].State.ToString());
        Assert.Equal($"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'application/json'.", writeList[1].State.ToString());
        Assert.Equal($"Selected input formatter '{typeof(TestInputFormatter)}' for content type 'application/json'.", writeList[2].State.ToString());
    }

    [Fact]
    public async Task BindModelAsync_LogsNoFormatterSelectedAndRemoveFromBodyAttribute()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var inputFormatters = new List<IInputFormatter>()
            {
                new TestInputFormatter(canRead: false),
                new TestInputFormatter(canRead: false),
            };

        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
        var bindingContext = GetBindingContext(typeof(Person), metadataProvider: provider);
        bindingContext.HttpContext.Request.ContentType = "multipart/form-data";
        bindingContext.BinderModelName = bindingContext.ModelName;
        var binder = new BodyModelBinder(inputFormatters, new TestHttpRequestStreamReaderFactory(), loggerFactory);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.Collection(
            sink.Writes,
            write => Assert.Equal(
                $"Attempting to bind model of type '{typeof(Person)}' using the name 'someName' in request data ...", write.State.ToString()),
            write => Assert.Equal(
                $"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'multipart/form-data'.", write.State.ToString()),
            write => Assert.Equal(
                $"Rejected input formatter '{typeof(TestInputFormatter)}' for content type 'multipart/form-data'.", write.State.ToString()),
            write => Assert.Equal(
                "No input formatter was found to support the content type 'multipart/form-data' for use with the [FromBody] attribute.", write.State.ToString()),
            write => Assert.Equal(
                $"To use model binding, remove the [FromBody] attribute from the property or parameter named '{bindingContext.ModelName}' with model type '{bindingContext.ModelType}'.", write.State.ToString()),
            write => Assert.Equal(
                $"Done attempting to bind model of type '{typeof(Person)}' using the name 'someName'.", write.State.ToString()));
    }

    [Fact]
    public async Task BindModelAsync_DoesNotThrowNullReferenceException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var provider = new TestModelMetadataProvider();
        provider.ForType<Person>().BindingDetails(d => d.BindingSource = BindingSource.Body);
        var bindingContext = GetBindingContext(
            typeof(Person),
            httpContext: httpContext,
            metadataProvider: provider);
        var binder = new BodyModelBinder(new List<IInputFormatter>(), new TestHttpRequestStreamReaderFactory());

        // Act & Assert (does not throw)
        await binder.BindModelAsync(bindingContext);
    }

    private static DefaultModelBindingContext GetBindingContext(
        Type modelType,
        HttpContext httpContext = null,
        IModelMetadataProvider metadataProvider = null)
    {
        if (httpContext == null)
        {
            httpContext = new DefaultHttpContext();
        }

        if (metadataProvider == null)
        {
            metadataProvider = new EmptyModelMetadataProvider();
        }

        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = httpContext,
            },
            FieldName = "someField",
            IsTopLevelObject = true,
            ModelMetadata = metadataProvider.GetMetadataForType(modelType),
            ModelName = "someName",
            ValueProvider = Mock.Of<IValueProvider>(),
            ModelState = new ModelStateDictionary(),
            BindingSource = BindingSource.Body,
        };

        return bindingContext;
    }

    private static BodyModelBinder CreateBinder(IList<IInputFormatter> formatters, bool treatEmptyInputAsDefaultValueOption = false)
    {
        var options = new MvcOptions();
        var binder = CreateBinder(formatters, options);
        binder.AllowEmptyBody = treatEmptyInputAsDefaultValueOption;

        return binder;
    }

    private static BodyModelBinder CreateBinder(IList<IInputFormatter> formatters, MvcOptions mvcOptions)
    {
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        return new BodyModelBinder(formatters, new TestHttpRequestStreamReaderFactory(), loggerFactory, mvcOptions);
    }

    private class XyzFormatter : TextInputFormatter
    {
        private readonly Func<InputFormatterContext, Encoding, Task<InputFormatterResult>> _readRequestBodyAsync;

        public XyzFormatter(Func<InputFormatterContext, Encoding, Task<InputFormatterResult>> readRequestBodyAsync)
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xyz"));
            SupportedEncodings.Add(Encoding.UTF8);
            _readRequestBodyAsync = readRequestBodyAsync;
        }

        protected override bool CanReadType(Type type)
        {
            return true;
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding effectiveEncoding)
        {
            return _readRequestBodyAsync(context, effectiveEncoding);
        }
    }

    private class TestInputFormatter : IInputFormatter
    {
        private readonly bool _canRead;

        public TestInputFormatter(bool canRead)
        {
            _canRead = canRead;
        }

        public bool CanRead(InputFormatterContext context)
        {
            return _canRead;
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            return InputFormatterResult.SuccessAsync(this);
        }
    }

    private class TestableJsonInputFormatter : NewtonsoftJsonInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public TestableJsonInputFormatter(bool throwNonInputFormatterException)
            : base(GetLogger(), new JsonSerializerSettings(), ArrayPool<char>.Shared, new DefaultObjectPoolProvider(), new MvcOptions(), new MvcNewtonsoftJsonOptions()
            {
                // The tests that use this class rely on the 2.1 behavior of this formatter.
                AllowInputFormatterExceptionMessages = true,
            })
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override InputFormatterExceptionPolicy ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private class TestableXmlSerializerInputFormatter : XmlSerializerInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public TestableXmlSerializerInputFormatter(bool throwNonInputFormatterException)
            : base(new MvcOptions())
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override InputFormatterExceptionPolicy ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private class TestableXmlDataContractSerializerInputFormatter : XmlDataContractSerializerInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public TestableXmlDataContractSerializerInputFormatter(bool throwNonInputFormatterException)
            : base(new MvcOptions())
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override InputFormatterExceptionPolicy ExceptionPolicy => InputFormatterExceptionPolicy.MalformedInputExceptions;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private class DerivedJsonInputFormatter : NewtonsoftJsonInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public DerivedJsonInputFormatter(bool throwNonInputFormatterException)
            : base(GetLogger(), new JsonSerializerSettings(), ArrayPool<char>.Shared, new DefaultObjectPoolProvider(), new MvcOptions(), new MvcNewtonsoftJsonOptions()
            {
                // The tests that use this class rely on the 2.1 behavior of this formatter.
                AllowInputFormatterExceptionMessages = true,
            })
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override InputFormatterExceptionPolicy ExceptionPolicy => InputFormatterExceptionPolicy.AllExceptions;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private class DerivedXmlSerializerInputFormatter : XmlSerializerInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public DerivedXmlSerializerInputFormatter(bool throwNonInputFormatterException)
            : base(new MvcOptions())
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override InputFormatterExceptionPolicy ExceptionPolicy => InputFormatterExceptionPolicy.AllExceptions;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private class DerivedXmlDataContractSerializerInputFormatter : XmlDataContractSerializerInputFormatter
    {
        private readonly bool _throwNonInputFormatterException;

        public DerivedXmlDataContractSerializerInputFormatter(bool throwNonInputFormatterException)
            : base(new MvcOptions())
        {
            _throwNonInputFormatterException = throwNonInputFormatterException;
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (_throwNonInputFormatterException)
            {
                throw new IOException("Unable to read input stream!!");
            }
            return base.ReadRequestBodyAsync(context, encoding);
        }
    }

    private static ILogger GetLogger()
    {
        return NullLogger.Instance;
    }

    // 'public' as XmlSerializer does not like non-public types
    public class Person
    {
        public string Name { get; set; }
    }
}
