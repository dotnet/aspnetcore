// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Abstract base class from which all XML Output Formatters derive from.
    /// </summary>
    public abstract class XmlOutputFormatter : OutputFormatter
    {
        public XmlOutputFormatter([NotNull] XmlWriterSettings xmlWriterSettings)
        {
            SupportedEncodings.Add(Encodings.UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(Encodings.UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

            WriterSettings = xmlWriterSettings;
        }

        /// <summary>
        /// Gets or sets the settings to be used by the XmlWriter.
        /// </summary>
        public XmlWriterSettings WriterSettings { get; private set; }

        /// <summary>
        /// Returns a serializer to serialzie the particualr type.
        /// </summary>
        /// <param name="type">The type which needs to be serialized.</param>
        /// <returns>The serializer object.</returns>
        public abstract object CreateSerializer(Type type);

        /// <inheritdoc />
        public override bool CanWriteResult([NotNull] OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            if (base.CanWriteResult(context, contentType))
            {
                if (CreateSerializer(GetObjectType(context)) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the default XmlWriterSettings.
        /// </summary>
        /// <returns>Default <see cref="XmlWriterSettings"/></returns>
        public static XmlWriterSettings GetDefaultXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                CloseOutput = false,
                CheckCharacters = false
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="XmlWriter"/> using the given stream and the WriterSettings.
        /// </summary>
        /// <param name="writeStream">The stream on which the XmlWriter should operate on.</param>
        /// <returns>A new instance of <see cref="XmlWriter"/></returns>
        public virtual XmlWriter CreateXmlWriter([NotNull] Stream writeStream,
                                                 [NotNull] XmlWriterSettings xmlWriterSettings)
        {
            return XmlWriter.Create(writeStream, xmlWriterSettings);
        }
    }
}