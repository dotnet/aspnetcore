// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// This class handles deserialization of input XML data
    /// to strongly-typed objects using <see cref="XmlSerializer"/>
    /// </summary>
    public class XmlSerializerInputFormatter : InputFormatter
    {
        private ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();
        private readonly XmlDictionaryReaderQuotas _readerQuotas = FormattingUtilities.GetDefaultXmlReaderQuotas();

        /// <summary>
        /// Initializes a new instance of XmlSerializerInputFormatter.
        /// </summary>
        public XmlSerializerInputFormatter()
        {
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            WrapperProviderFactories = new List<IWrapperProviderFactory>();
            WrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());
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
        public XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas
        {
            get { return _readerQuotas; }
        }

        /// <summary>
        /// Reads the input XML.
        /// </summary>
        /// <param name="context">The input formatter context which contains the body to be read.</param>
        /// <returns>Task which reads the input.</returns>
        public override Task<object> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;

            using (var xmlReader = CreateXmlReader(new NonDisposableStream(request.Body)))
            {
                var type = GetSerializableType(context.ModelType);

                var serializer = GetCachedSerializer(type);

                var deserializedObject = serializer.Deserialize(xmlReader);

                // Unwrap only if the original type was wrapped.
                if (type != context.ModelType)
                {
                    var unwrappable = deserializedObject as IUnwrappable;
                    if (unwrappable != null)
                    {
                        deserializedObject = unwrappable.Unwrap(declaredType: context.ModelType);
                    }
                }

                return Task.FromResult(deserializedObject);
            }
        }

        /// <inheritdoc />
        protected override bool CanReadType([NotNull] Type type)
        {
            return GetCachedSerializer(GetSerializableType(type)) != null;
        }

        /// <summary>
        /// Gets the type to which the XML will be deserialized.
        /// </summary>
        /// <param name="declaredType">The declared type.</param>
        /// <returns>The type to which the XML will be deserialized.</returns>
        protected virtual Type GetSerializableType([NotNull] Type declaredType)
        {
            var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(
                                                    new WrapperProviderContext(declaredType, isSerialization: false));

            return wrapperProvider?.WrappingType ?? declaredType;
        }

        /// <summary>
        /// Called during deserialization to get the <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="readStream">The <see cref="Stream"/> from which to read.</param>
        /// <returns>The <see cref="XmlReader"/> used during deserialization.</returns>
        protected virtual XmlReader CreateXmlReader([NotNull] Stream readStream)
        {
            return XmlDictionaryReader.CreateTextReader(
                readStream, _readerQuotas);
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
            object serializer;
            if (!_serializerCache.TryGetValue(type, out serializer))
            {
                serializer = CreateSerializer(type);
                if (serializer != null)
                {
                    _serializerCache.TryAdd(type, serializer);
                }
            }

            return (XmlSerializer) serializer;
        }
    }
}