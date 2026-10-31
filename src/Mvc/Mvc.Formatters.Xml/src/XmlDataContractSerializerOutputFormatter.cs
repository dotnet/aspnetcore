// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// This class handles serialization of objects
/// to XML using <see cref="DataContractSerializer"/>
/// </summary>
public partial class XmlDataContractSerializerOutputFormatter : TextOutputFormatter
{
    private readonly ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();
    private readonly ILogger _logger;
    private DataContractSerializerSettings _serializerSettings;
    private MvcOptions? _mvcOptions;
    private AsyncEnumerableReader? _asyncEnumerableReaderFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>
    /// with default <see cref="XmlWriterSettings"/>.
    /// </summary>
    public XmlDataContractSerializerOutputFormatter()
        : this(FormattingUtilities.GetDefaultXmlWriterSettings())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>
    /// with default <see cref="XmlWriterSettings"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public XmlDataContractSerializerOutputFormatter(ILoggerFactory loggerFactory)
        : this(FormattingUtilities.GetDefaultXmlWriterSettings(), loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>.
    /// </summary>
    /// <param name="writerSettings">The settings to be used by the <see cref="DataContractSerializer"/>.</param>
    public XmlDataContractSerializerOutputFormatter(XmlWriterSettings writerSettings)
        : this(writerSettings, loggerFactory: NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>.
    /// </summary>
    /// <param name="writerSettings">The settings to be used by the <see cref="DataContractSerializer"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public XmlDataContractSerializerOutputFormatter(XmlWriterSettings writerSettings, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(writerSettings);

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);

        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationXml);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.TextXml);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyXmlSyntax);

        WriterSettings = writerSettings;

        _serializerSettings = new DataContractSerializerSettings();

        WrapperProviderFactories = new List<IWrapperProviderFactory>()
            {
                new SerializableErrorWrapperProviderFactory(),
            };
        WrapperProviderFactories.Add(new EnumerableWrapperProviderFactory(WrapperProviderFactories));

        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the list of <see cref="IWrapperProviderFactory"/> to
    /// provide the wrapping type for serialization.
    /// </summary>
    public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }

    /// <summary>
    /// Gets the settings to be used by the XmlWriter.
    /// </summary>
    public XmlWriterSettings WriterSettings { get; }

    /// <summary>
    /// Gets or sets the <see cref="DataContractSerializerSettings"/> used to configure the
    /// <see cref="DataContractSerializer"/>.
    /// </summary>
    public DataContractSerializerSettings SerializerSettings
    {
        get => _serializerSettings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _serializerSettings = value;
        }
    }

    /// <summary>
    /// Gets the type to be serialized.
    /// </summary>
    /// <param name="type">The original type to be serialized</param>
    /// <returns>The original or wrapped type provided by any <see cref="IWrapperProvider"/>s.</returns>
    protected virtual Type GetSerializableType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(new WrapperProviderContext(
            type,
            isSerialization: true));

        return wrapperProvider?.WrappingType ?? type;
    }

    /// <inheritdoc />
    protected override bool CanWriteType(Type? type)
    {
        if (type == null)
        {
            return false;
        }

        return GetCachedSerializer(GetSerializableType(type)) != null;
    }

    /// <summary>
    /// Create a new instance of <see cref="DataContractSerializer"/> for the given object type.
    /// </summary>
    /// <param name="type">The type of object for which the serializer should be created.</param>
    /// <returns>A new instance of <see cref="DataContractSerializer"/></returns>
    protected virtual DataContractSerializer? CreateSerializer(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        try
        {
            // Verify that type is a valid data contract by forcing the serializer to try to create a data contract
            FormattingUtilities.XsdDataContractExporter.GetRootElementName(type);

            // If the serializer does not support this type it will throw an exception.
            return new DataContractSerializer(type, _serializerSettings);
        }
        catch (Exception ex)
        {
            Log.FailedToCreateDataContractSerializer(_logger, type.FullName!, ex);

            // We do not surface the caught exception because if CanWriteResult returns
            // false, then this Formatter is not picked up at all.
            return null;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="XmlWriter"/> using the given <see cref="TextWriter"/> and
    /// <see cref="XmlWriterSettings"/>.
    /// </summary>
    /// <param name="writer">
    /// The underlying <see cref="TextWriter"/> which the <see cref="XmlWriter"/> should write to.
    /// </param>
    /// <param name="xmlWriterSettings">
    /// The <see cref="XmlWriterSettings"/>.
    /// </param>
    /// <returns>A new instance of <see cref="XmlWriter"/></returns>
    public virtual XmlWriter CreateXmlWriter(
        TextWriter writer,
        XmlWriterSettings xmlWriterSettings)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(xmlWriterSettings);

        // We always close the TextWriter, so the XmlWriter shouldn't.
        xmlWriterSettings.CloseOutput = false;

        return XmlWriter.Create(writer, xmlWriterSettings);
    }

    /// <summary>
    /// Creates a new instance of <see cref="XmlWriter"/> using the given <see cref="TextWriter"/> and
    /// <see cref="XmlWriterSettings"/>.
    /// </summary>
    /// <param name="context">The formatter context associated with the call.</param>
    /// <param name="writer">
    /// The underlying <see cref="TextWriter"/> which the <see cref="XmlWriter"/> should write to.
    /// </param>
    /// <param name="xmlWriterSettings">
    /// The <see cref="XmlWriterSettings"/>.
    /// </param>
    /// <returns>A new instance of <see cref="XmlWriter"/>.</returns>
    public virtual XmlWriter CreateXmlWriter(
        OutputFormatterWriteContext context,
        TextWriter writer,
        XmlWriterSettings xmlWriterSettings)
    {
        return CreateXmlWriter(writer, xmlWriterSettings);
    }

    /// <inheritdoc />
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        var writerSettings = WriterSettings.Clone();
        writerSettings.Encoding = selectedEncoding;

        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        _mvcOptions ??= httpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value;
        _asyncEnumerableReaderFactory ??= new AsyncEnumerableReader(_mvcOptions);

        var value = context.Object;
        var valueType = context.ObjectType;

        if (value is not null && _asyncEnumerableReaderFactory.TryGetReader(value.GetType(), out var reader))
        {
            Log.BufferingAsyncEnumerable(_logger, value);

            value = await reader(value, context.HttpContext.RequestAborted);
            valueType = value.GetType();
            if (context.HttpContext.RequestAborted.IsCancellationRequested)
            {
                return;
            }
        }

        Debug.Assert(valueType is not null);
        // Wrap the object only if there is a wrapping type.
        var wrappingType = GetSerializableType(valueType);
        if (wrappingType != null && wrappingType != valueType)
        {
            var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(new WrapperProviderContext(
                declaredType: valueType,
                isSerialization: true));

            Debug.Assert(wrapperProvider is not null);

            value = wrapperProvider.Wrap(value);
        }

        var dataContractSerializer = GetCachedSerializer(wrappingType!);

        var responseStream = response.Body;
        FileBufferingWriteStream? fileBufferingWriteStream = null;
        if (!_mvcOptions.SuppressOutputFormatterBuffering)
        {
            fileBufferingWriteStream = new FileBufferingWriteStream();
            responseStream = fileBufferingWriteStream;
        }

        try
        {
            await using (var textWriter = context.WriterFactory(responseStream, writerSettings.Encoding))
            {
                using var xmlWriter = CreateXmlWriter(context, textWriter, writerSettings);
                dataContractSerializer.WriteObject(xmlWriter, value);
            }

            if (fileBufferingWriteStream != null)
            {
                response.ContentLength = fileBufferingWriteStream.Length;
                await fileBufferingWriteStream.DrainBufferAsync(response.Body);
            }
        }
        finally
        {
            if (fileBufferingWriteStream != null)
            {
                await fileBufferingWriteStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Gets the cached serializer or creates and caches the serializer for the given type.
    /// </summary>
    /// <returns>The <see cref="DataContractSerializer"/> instance.</returns>
    protected virtual DataContractSerializer GetCachedSerializer(Type type)
    {
        if (!_serializerCache.TryGetValue(type, out var serializer))
        {
            serializer = CreateSerializer(type);
            if (serializer != null)
            {
                _serializerCache.TryAdd(type, serializer);
            }
        }

        return (DataContractSerializer)serializer!;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Buffering IAsyncEnumerable instance of type '{Type}'.", EventName = "BufferingAsyncEnumerable", SkipEnabledCheck = true)]
        private static partial void BufferingAsyncEnumerable(ILogger logger, string type);

        public static void BufferingAsyncEnumerable(ILogger logger, object asyncEnumerable)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                BufferingAsyncEnumerable(logger, asyncEnumerable.GetType().FullName!);
            }
        }

        [LoggerMessage(2, LogLevel.Warning, "An error occurred while trying to create a DataContractSerializer for the type '{Type}'.", EventName = "FailedToCreateDataContractSerializer")]
        public static partial void FailedToCreateDataContractSerializer(ILogger logger, string type, Exception exception);
    }
}
