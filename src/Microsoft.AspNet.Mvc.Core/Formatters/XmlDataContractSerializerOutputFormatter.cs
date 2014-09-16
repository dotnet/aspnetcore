// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// This class handles serialization of objects
    /// to XML using <see cref="DataContractSerializer"/>
    /// </summary>
    public class XmlDataContractSerializerOutputFormatter : XmlOutputFormatter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>
        /// with default XmlWriterSettings
        /// </summary>
        public XmlDataContractSerializerOutputFormatter()
            : this(GetDefaultXmlWriterSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="XmlDataContractSerializerOutputFormatter"/>
        /// </summary>
        /// <param name="writerSettings">The settings to be used by the <see cref="DataContractSerializer"/>.</param>
        public XmlDataContractSerializerOutputFormatter([NotNull] XmlWriterSettings writerSettings)
            : base(writerSettings)
        {
        }

        /// <inheritdoc />
        protected override bool CanWriteType(Type declaredType, Type runtimeType)
        {
            return CreateSerializer(GetSerializableType(declaredType, runtimeType)) != null;
        }

        /// <summary>
        /// Create a new instance of <see cref="DataContractSerializer"/> for the given object type.
        /// </summary>
        /// <param name="type">The type of object for which the serializer should be created.</param>
        /// <returns>A new instance of <see cref="DataContractSerializer"/></returns>
        protected virtual DataContractSerializer CreateSerializer([NotNull] Type type)
        {
            try
            {
#if ASPNET50
                // Verify that type is a valid data contract by forcing the serializer to try to create a data contract
                FormattingUtilities.XsdDataContractExporter.GetRootElementName(type);
#endif
                // If the serializer does not support this type it will throw an exception.
                return new DataContractSerializer(type);
            }
            catch (Exception)
            {
                // We do not surface the caught exception because if CanWriteResult returns
                // false, then this Formatter is not picked up at all.
                return null;
            }
        }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync([NotNull] OutputFormatterContext context)
        {
            var tempWriterSettings = WriterSettings.Clone();
            tempWriterSettings.Encoding = context.SelectedEncoding;

            var innerStream = context.ActionContext.HttpContext.Response.Body;

            using (var outputStream = new DelegatingStream(innerStream))
            using (var xmlWriter = CreateXmlWriter(outputStream, tempWriterSettings))
            {
                var runtimeType = context.Object == null ? null : context.Object.GetType();

                var type = GetSerializableType(context.DeclaredType, runtimeType);
                var dataContractSerializer = CreateSerializer(type);
                dataContractSerializer.WriteObject(xmlWriter, context.Object);
            }

            return Task.FromResult(true);
        }
    }
}
