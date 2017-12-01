// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// This class handles serialization of objects
    /// to XML using <see cref="DataContractSerializer"/>
    /// </summary>
    public class XmlDataContractSerializerOutputFormatter : TextOutputFormatter
    {
        private readonly ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();
        private readonly ILogger _logger;
        private DataContractSerializerSettings _serializerSettings;

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
            : this(writerSettings, loggerFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>.
        /// </summary>
        /// <param name="writerSettings">The settings to be used by the <see cref="DataContractSerializer"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public XmlDataContractSerializerOutputFormatter(XmlWriterSettings writerSettings, ILoggerFactory loggerFactory)
        {
            if (writerSettings == null)
            {
                throw new ArgumentNullException(nameof(writerSettings));
            }

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyXmlSyntax);

            WriterSettings = writerSettings;

            _serializerSettings = new DataContractSerializerSettings();

            WrapperProviderFactories = new List<IWrapperProviderFactory>();
            WrapperProviderFactories.Add(new EnumerableWrapperProviderFactory(WrapperProviderFactories));
            WrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());

            _logger = loggerFactory?.CreateLogger(GetType());
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
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(new WrapperProviderContext(
                type,
                isSerialization: true));

            return wrapperProvider?.WrappingType ?? type;
        }

        /// <inheritdoc />
        protected override bool CanWriteType(Type type)
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
        protected virtual DataContractSerializer CreateSerializer(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            try
            {
                // Verify that type is a valid data contract by forcing the serializer to try to create a data contract
                FormattingUtilities.XsdDataContractExporter.GetRootElementName(type);

                // If the serializer does not support this type it will throw an exception.
                return new DataContractSerializer(type, _serializerSettings);
            }
            catch (Exception ex)
            {
                _logger?.FailedToCreateDataContractSerializer(type.FullName, ex);

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
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (xmlWriterSettings == null)
            {
                throw new ArgumentNullException(nameof(xmlWriterSettings));
            }

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            var writerSettings = WriterSettings.Clone();
            writerSettings.Encoding = selectedEncoding;

            // Wrap the object only if there is a wrapping type.
            var value = context.Object;
            var wrappingType = GetSerializableType(context.ObjectType);
            if (wrappingType != null && wrappingType != context.ObjectType)
            {
                var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(new WrapperProviderContext(
                    declaredType: context.ObjectType,
                    isSerialization: true));

                value = wrapperProvider.Wrap(value);
            }

            var dataContractSerializer = GetCachedSerializer(wrappingType);

            using (var textWriter = context.WriterFactory(context.HttpContext.Response.Body, writerSettings.Encoding))
            {
                using (var xmlWriter = CreateXmlWriter(context, textWriter, writerSettings))
                {
                    dataContractSerializer.WriteObject(xmlWriter, value);
                }

                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous 
                // write).
                await textWriter.FlushAsync();
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

            return (DataContractSerializer)serializer;
        }
    }
}
