// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Xml
{
    /// <summary>
    /// This class handles deserialization of input XML data
    /// to strongly-typed objects using <see cref="DataContractSerializer"/>.
    /// </summary>
    public class XmlDataContractSerializerInputFormatter : IInputFormatter
    {
        private DataContractSerializerSettings _serializerSettings;
        private readonly XmlDictionaryReaderQuotas _readerQuotas = FormattingUtilities.GetDefaultXmlReaderQuotas();

        /// <summary>
        /// Initializes a new instance of DataContractSerializerInputFormatter
        /// </summary>
        public XmlDataContractSerializerInputFormatter()
        {
            SupportedEncodings = new List<Encoding>();
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);

            SupportedMediaTypes = new List<MediaTypeHeaderValue>();
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            _serializerSettings = new DataContractSerializerSettings();

            WrapperProviderFactories = new List<IWrapperProviderFactory>();
            WrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());
        }

        /// <summary>
        /// Gets the list of <see cref="IWrapperProviderFactory"/> to
        /// provide the wrapping type for de-serialization.
        /// </summary>
        public IList<IWrapperProviderFactory> WrapperProviderFactories { get; }

        /// <inheritdoc />
        public IList<MediaTypeHeaderValue> SupportedMediaTypes { get; }

        /// <inheritdoc />
        public IList<Encoding> SupportedEncodings { get; }

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

        /// <inheritdoc />
        public bool CanRead(InputFormatterContext context)
        {
            var contentType = context.ActionContext.HttpContext.Request.ContentType;
            MediaTypeHeaderValue requestContentType;
            if (!MediaTypeHeaderValue.TryParse(contentType, out requestContentType))
            {
                return false;
            }

            return SupportedMediaTypes
                            .Any(supportedMediaType => supportedMediaType.IsSubsetOf(requestContentType));
        }

        /// <summary>
        /// Gets or sets the <see cref="DataContractSerializerSettings"/> used to configure the 
        /// <see cref="DataContractSerializer"/>.
        /// </summary>
        public DataContractSerializerSettings SerializerSettings
        {
            get { return _serializerSettings; }
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
        /// Reads the input XML.
        /// </summary>
        /// <param name="context">The input formatter context which contains the body to be read.</param>
        /// <returns>Task which reads the input.</returns>
        public async Task<object> ReadAsync(InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                return GetDefaultValueForType(context.ModelType);
            }

            return await ReadInternalAsync(context);
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
        /// Called during deserialization to get the <see cref="DataContractSerializer"/>.
        /// </summary>
        /// <param name="type">The type of object for which the serializer should be created.</param>
        /// <returns>The <see cref="DataContractSerializer"/> used during deserialization.</returns>
        protected virtual DataContractSerializer CreateSerializer([NotNull] Type type)
        {
            return new DataContractSerializer(type, _serializerSettings);
        }

        private object GetDefaultValueForType(Type modelType)
        {
            if (modelType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(modelType);
            }

            return null;
        }

        private Task<object> ReadInternalAsync(InputFormatterContext context)
        {
            var request = context.ActionContext.HttpContext.Request;

            using (var xmlReader = CreateXmlReader(new DelegatingStream(request.Body)))
            {
                var type = GetSerializableType(context.ModelType);

                var serializer = CreateSerializer(type);

                var deserializedObject = serializer.ReadObject(xmlReader);

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
    }
}