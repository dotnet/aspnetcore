// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// This class handles deserialization of input XML data
    /// to strongly-typed objects using <see cref="XmlSerializer"/>
    /// </summary>
    public class XmlSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
    {
        private const int DefaultMemoryThreshold = 1024 * 30;
        private readonly ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();
        private readonly XmlDictionaryReaderQuotas _readerQuotas = FormattingUtilities.GetDefaultXmlReaderQuotas();
        private readonly MvcOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerInputFormatter"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public XmlSerializerInputFormatter(MvcOptions options)
        {
            _options = options;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyXmlSyntax);

            WrapperProviderFactories = new List<IWrapperProviderFactory>
            {
                new SerializableErrorWrapperProviderFactory(),
            };
        }

        /// <summary>
        /// Gets the list of <see cref="IWrapperProviderFactory"/> to
        /// provide the wrapping type for de-serialization.
        /// </summary>
        public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }

        /// <summary>
        /// Indicates the acceptable input XML depth.
        /// </summary>
        public int MaxDepth
        {
            get { return _readerQuotas.MaxDepth; }
            set { _readerQuotas.MaxDepth = value; }
        }

        /// <summary>
        /// The quotas include - DefaultMaxDepth, DefaultMaxStringContentLength, DefaultMaxArrayLength,
        /// DefaultMaxBytesPerRead, DefaultMaxNameTableCharCount
        /// </summary>
        public XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas => _readerQuotas;

        /// <inheritdoc />
        public virtual InputFormatterExceptionPolicy ExceptionPolicy
        {
            get
            {
                if (GetType() == typeof(XmlSerializerInputFormatter))
                {
                    return InputFormatterExceptionPolicy.MalformedInputExceptions;
                }
                return InputFormatterExceptionPolicy.AllExceptions;
            }
        }

        /// <inheritdoc />
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var request = context.HttpContext.Request;
            Stream readStream = new NonDisposableStream(request.Body);
            var disposeReadStream = false;

            if (readStream.CanSeek)
            {
                // The most common way of getting here is the user has request buffering on.
                // However, request buffering isn't eager, and consequently it will peform pass-thru synchronous
                // reads as part of the deserialization.
                // To avoid this, drain and reset the stream.
                var position = request.Body.Position;
                await readStream.DrainAsync(CancellationToken.None);
                readStream.Position = position;
            }
            else if (!_options.SuppressInputFormatterBuffering)
            {
                // XmlSerializer does synchronous reads. In order to avoid blocking on the stream, we asynchronously
                // read everything into a buffer, and then seek back to the beginning.
                var memoryThreshold = DefaultMemoryThreshold;
                var contentLength = request.ContentLength.GetValueOrDefault();
                if (contentLength > 0 && contentLength < memoryThreshold)
                {
                    // If the Content-Length is known and is smaller than the default buffer size, use it.
                    memoryThreshold = (int)contentLength;
                }

                readStream = new FileBufferingReadStream(request.Body, memoryThreshold);

                await readStream.DrainAsync(CancellationToken.None);
                readStream.Seek(0L, SeekOrigin.Begin);
                disposeReadStream = true;
            }

            try
            {
                var type = GetSerializableType(context.ModelType);                
                using var xmlReader = CreateXmlReader(readStream, encoding, type);

                var serializer = GetCachedSerializer(type);

                var deserializedObject = serializer.Deserialize(xmlReader);

                // Unwrap only if the original type was wrapped.
                if (type != context.ModelType)
                {
                    if (deserializedObject is IUnwrappable unwrappable)
                    {
                        deserializedObject = unwrappable.Unwrap(declaredType: context.ModelType);
                    }
                }

                return InputFormatterResult.Success(deserializedObject);
            }
            // XmlSerializer wraps actual exceptions (like FormatException or XmlException) into an InvalidOperationException
            // https://github.com/dotnet/corefx/blob/master/src/System.Private.Xml/src/System/Xml/Serialization/XmlSerializer.cs#L652
            catch (InvalidOperationException exception) when (exception.InnerException != null &&
                exception.InnerException.InnerException == null &&
                string.Equals("Microsoft.GeneratedCode", exception.InnerException.Source, StringComparison.InvariantCulture))
            {
                // Know this was an XML parsing error because the inner Exception was thrown in the (generated)
                // assembly the XmlSerializer uses for parsing. The problem did not arise lower in the stack i.e. it's
                // not (for example) an out-of-memory condition.
                throw new InputFormatterException(Resources.ErrorDeserializingInputData, exception.InnerException);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is FormatException ||
                exception.InnerException is XmlException)
            {
                throw new InputFormatterException(Resources.ErrorDeserializingInputData, exception.InnerException);
            }
            finally
            {
                if (disposeReadStream)
                {
                    await readStream.DisposeAsync();
                }
            }
        }

        /// <inheritdoc />
        protected override bool CanReadType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return GetCachedSerializer(GetSerializableType(type)) != null;
        }

        /// <summary>
        /// Gets the type to which the XML will be deserialized.
        /// </summary>
        /// <param name="declaredType">The declared type.</param>
        /// <returns>The type to which the XML will be deserialized.</returns>
        protected virtual Type GetSerializableType(Type declaredType)
        {
            if (declaredType == null)
            {
                throw new ArgumentNullException(nameof(declaredType));
            }

            var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(
                                                    new WrapperProviderContext(declaredType, isSerialization: false));

            return wrapperProvider?.WrappingType ?? declaredType;
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="readStream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="encoding">The <see cref="Encoding"/> used to read the stream.</param>
        /// <param name="type">The <see cref="Type"/> that is to be deserialized.</param>
        /// <returns>The <see cref="XmlReader"/> used during deserialization.</returns>
        protected virtual XmlReader CreateXmlReader(Stream readStream, Encoding encoding, Type type)
        {
            return CreateXmlReader(readStream, encoding);
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="readStream">The <see cref="Stream"/> from which to read.</param>
        /// <param name="encoding">The <see cref="Encoding"/> used to read the stream.</param>
        /// <returns>The <see cref="XmlReader"/> used during deserialization.</returns>
        protected virtual XmlReader CreateXmlReader(Stream readStream, Encoding encoding)
        {
            if (readStream == null)
            {
                throw new ArgumentNullException(nameof(readStream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return XmlDictionaryReader.CreateTextReader(readStream, encoding, _readerQuotas, onClose: null);
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="XmlSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="XmlSerializer"/> used during deserialization.</returns>
        protected virtual XmlSerializer CreateSerializer(Type type)
        {
            try
            {
                // If the serializer does not support this type it will throw an exception.
                return new XmlSerializer(type);
            }
            catch (Exception)
            {
                // We do not surface the caught exception because if CanRead returns
                // false, then this Formatter is not picked up at all.
                return null;
            }
        }

        /// <summary>
        /// Gets the cached serializer or creates and caches the serializer for the given type.
        /// </summary>
        /// <returns>The <see cref="XmlSerializer"/> instance.</returns>
        protected virtual XmlSerializer GetCachedSerializer(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!_serializerCache.TryGetValue(type, out var serializer))
            {
                serializer = CreateSerializer(type);
                if (serializer != null)
                {
                    _serializerCache.TryAdd(type, serializer);
                }
            }

            return (XmlSerializer)serializer;
        }
    }
}
