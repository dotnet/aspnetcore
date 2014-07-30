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

        /// <summary>
        /// Create a new instance of <see cref="DataContractSerializer"/> for the given object type.
        /// </summary>
        /// <param name="type">The type of object for which the serializer should be created.</param>
        /// <returns>A new instance of <see cref="DataContractSerializer"/></returns>
        public virtual DataContractSerializer CreateDataContractSerializer([NotNull] Type type)
        {
            return new DataContractSerializer(type);
        }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync([NotNull] OutputFormatterContext context)
        {
            var response = context.ActionContext.HttpContext.Response;

            var tempWriterSettings = WriterSettings.Clone();
            tempWriterSettings.Encoding = context.SelectedEncoding;
            using (var xmlWriter = CreateXmlWriter(response.Body, tempWriterSettings))
            {
                var dataContractSerializer = CreateDataContractSerializer(context.DeclaredType);
                dataContractSerializer.WriteObject(xmlWriter, context.Object);
            }

            return Task.FromResult(true);
        }
    }
}