// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// This class handles deserialization of input XML data
    /// to strongly-typed objects using <see cref="XmlSerializer"/>
    /// </summary>
    public class XmlSerializerInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
    {
        private readonly ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();
        private readonly XmlDictionaryReaderQuotas _readerQuotas = FormattingUtilities.GetDefaultXmlReaderQuotas();
        private readonly bool _suppressInputFormatterBuffering;
        private readonly MvcOptions _options;

        /// <summary>
        /// Initializes a new instance of XmlSerializerInputFormatter.
        /// </summary>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public XmlSerializerInputFormatter()
        {
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.TextXml);
            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyXmlSyntax);

            WrapperProviderFactories = new List<IWrapperProviderFactory>();
            WrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerInputFormatter"/>.
        /// </summary>
        /// <param name="suppressInputFormatterBuffering">Flag to buffer entire request body before deserializing it.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public XmlSerializerInputFormatter(bool suppressInputFormatterBuffering)
            : this()
        {
            _suppressInputFormatterBuffering = suppressInputFormatterBuffering;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerInputFormatter"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public XmlSerializerInputFormatter(MvcOptions options)
#pragma warning disable CS0618
            : this()
#pragma warning restore CS0618
        {
            _options = options;
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
        public virtual InputFormatterExceptionModelStatePolicy ExceptionPolicy
        {
            get
            {
                if (GetType() == typeof(XmlSerializerInputFormatter))
                {
                    return InputFormatterExceptionModelStatePolicy.MalformedInputExceptions;
                }
                return InputFormatterExceptionModelStatePolicy.AllExceptions;
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

            var suppressInputFormatterBuffering = _options?.SuppressInputFormatterBuffering ?? _suppressInputFormatterBuffering;

            if (!request.Body.CanSeek && !suppressInputFormatterBuffering)
            {
                // XmlSerializer does synchronous reads. In order to avoid blocking on the stream, we asynchronously 
                // read everything into a buffer, and then seek back to the beginning. 
                BufferingHelper.EnableRewind(request);
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(CancellationToken.None);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            try
            {
                using (var xmlReader = CreateXmlReader(new NonDisposableStream(request.Body), encoding))
                {
                    var type = GetSerializableType(context.ModelType);

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
            }
            // XmlSerializer wraps actual exceptions (like FormatException or XmlException) into an InvalidOperationException
            // https://github.com/dotnet/corefx/blob/master/src/System.Private.Xml/src/System/Xml/Serialization/XmlSerializer.cs#L652
            catch (InvalidOperationException exception) when (exception.InnerException is FormatException || exception.InnerException is XmlException)
            {
                throw new InputFormatterException(Resources.ErrorDeserializingInputData, exception);
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