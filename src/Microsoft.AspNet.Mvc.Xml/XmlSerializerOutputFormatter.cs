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
    /// This class handles serialization of objects
    /// to XML using <see cref="XmlSerializer"/>
    /// </summary>
    public class XmlSerializerOutputFormatter : OutputFormatter
    {
        private ConcurrentDictionary<Type, object> _serializerCache = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerOutputFormatter"/>
        /// with default XmlWriterSettings.
        /// </summary>
        public XmlSerializerOutputFormatter() :
            this(FormattingUtilities.GetDefaultXmlWriterSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XmlSerializerOutputFormatter"/>
        /// </summary>
        /// <param name="writerSettings">The settings to be used by the <see cref="XmlSerializer"/>.</param>
        public XmlSerializerOutputFormatter([NotNull] XmlWriterSettings writerSettings)
        {
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            WriterSettings = writerSettings;

            WrapperProviderFactories = new List<IWrapperProviderFactory>();
            WrapperProviderFactories.Add(new EnumerableWrapperProviderFactory(WrapperProviderFactories));
            WrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());
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
        /// Gets the type of the object to be serialized.
        /// </summary>
        /// <param name="declaredType">The declared type of the object.</param>
        /// <param name="runtimeType">The runtime type of the object</param>
        /// <returns>A type that needs to be serialized.</returns>
        protected virtual Type ResolveType(Type declaredType, Type runtimeType)
        {
            if (declaredType == null || declaredType == typeof(object))
            {
                if (runtimeType != null)
                {
                    return runtimeType;
                }
            }

            return declaredType;
        }

        /// <summary>
        /// Gets the type to be serialized.
        /// </summary>
        /// <param name="type">The original type to be serialized</param>
        /// <returns>The original or wrapped type provided by any <see cref="IWrapperProvider"/>.</returns>
        protected virtual Type GetSerializableType(Type type)
        {
            var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(
                                                        new WrapperProviderContext(type, isSerialization: true));

            return wrapperProvider?.WrappingType ?? type;
        }

        /// <inheritdoc />
        protected override bool CanWriteType(Type declaredType, Type runtimeType)
        {
            var type = ResolveType(declaredType, runtimeType);
            if (type == null)
            {
                return false;
            }

            return GetCachedSerializer(GetSerializableType(type)) != null;
        }

        /// <summary>
        /// Create a new instance of <see cref="XmlSerializer"/> for the given object type.
        /// </summary>
        /// <param name="type">The type of object for which the serializer should be created.</param>
        /// <returns>A new instance of <see cref="XmlSerializer"/></returns>
        protected virtual XmlSerializer CreateSerializer([NotNull] Type type)
        {
            try
            {
                // If the serializer does not support this type it will throw an exception.
                return new XmlSerializer(type);
            }
            catch (Exception)
            {
                // We do not surface the caught exception because if CanWriteResult returns
                // false, then this Formatter is not picked up at all.
                return null;
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="XmlWriter"/> using the given stream and the <see cref="WriterSettings"/>.
        /// </summary>
        /// <param name="writeStream">The stream on which the XmlWriter should operate on.</param>
        /// <returns>A new instance of <see cref="XmlWriter"/></returns>
        public virtual XmlWriter CreateXmlWriter([NotNull] Stream writeStream,
                                                 [NotNull] XmlWriterSettings xmlWriterSettings)
        {
            return XmlWriter.Create(writeStream, xmlWriterSettings);
        }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync([NotNull] OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;

            var tempWriterSettings = WriterSettings.Clone();
            tempWriterSettings.Encoding = context.SelectedEncoding;

            var innerStream = context.ActionContext.HttpContext.Response.Body;

            using (var outputStream = new NonDisposableStream(innerStream))
            using (var xmlWriter = CreateXmlWriter(outputStream, tempWriterSettings))
            {
                var obj = context.Object;
                var runtimeType = obj?.GetType();

                var resolvedType = ResolveType(context.DeclaredType, runtimeType);

                var wrappingType = GetSerializableType(resolvedType);

                // Wrap the object only if there is a wrapping type.
                if (wrappingType != null && wrappingType != resolvedType)
                {
                    var wrapperProvider = WrapperProviderFactories.GetWrapperProvider(
                                                            new WrapperProviderContext(
                                                                                declaredType: resolvedType,
                                                                                isSerialization: true));

                    obj = wrapperProvider.Wrap(obj);
                }

                var xmlSerializer = GetCachedSerializer(wrappingType);
                xmlSerializer.Serialize(xmlWriter, obj);
            }

            return Task.FromResult(true);
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

            return (XmlSerializer)serializer;
        }
    }
}