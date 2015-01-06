// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Defines a serializable container for storing ModelState information.
    /// This information is stored as key/value pairs.
    /// </summary>
    [XmlRoot("Error")]
    public sealed class SerializableError : Dictionary<string, object>, IXmlSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableError"/> class.
        /// </summary>
        public SerializableError()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="SerializableError"/>.
        /// </summary>
        /// <param name="modelState"><see cref="ModelState"/> containing the validation errors.</param>
        public SerializableError([NotNull] ModelStateDictionary modelState)
            : this()
        {
            if (modelState.IsValid)
            {
                return;
            }
            
            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    var errorMessages = errors.Select(error =>
                    {
                        return string.IsNullOrEmpty(error.ErrorMessage) ?
                            Resources.SerializableError_DefaultError : error.ErrorMessage;
                    }).ToArray();

                    Add(key, errorMessages);
                }
            }
        }

        // <inheritdoc />
        public XmlSchema GetSchema()
        {
            return null;
        }

        // <inheritdoc />
        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var key = XmlConvert.DecodeName(reader.LocalName);
                var value = reader.ReadInnerXml();

                Add(key, value);
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        // <inheritdoc />
        public void WriteXml(XmlWriter writer)
        {
            foreach (var keyValuePair in this)
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;
                writer.WriteStartElement(XmlConvert.EncodeLocalName(key));
                if (value != null)
                {
                    writer.WriteValue(value);
                }

                writer.WriteEndElement();
            }
        }
    }
}