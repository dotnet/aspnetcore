// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    /// <summary>
    /// Wrapper class for <see cref="ValidationProblemDetails"/> to enable it to be serialized by the xml formatters.
    /// </summary>
    [XmlRoot(nameof(ValidationProblemDetails))]
    [Obsolete("This type is deprecated and will be removed in a future version")]
    public class ValidationProblemDetails21Wrapper : ProblemDetails21Wrapper, IUnwrappable
    {
        private static readonly string ErrorKey = "MVC-Errors";

        /// <summary>
        /// Initializes a new instance of <see cref="ValidationProblemDetailsWrapper"/>.
        /// </summary>
        public ValidationProblemDetails21Wrapper()
            : this(new ValidationProblemDetails())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ValidationProblemDetailsWrapper"/> for the specified
        /// <paramref name="problemDetails"/>.
        /// </summary>
        /// <param name="problemDetails">The <see cref="ProblemDetails"/>.</param>
        public ValidationProblemDetails21Wrapper(ValidationProblemDetails problemDetails)
            : base(problemDetails)
        {
            ProblemDetails = problemDetails;
        }

        internal new ValidationProblemDetails ProblemDetails { get; }

        /// <inheritdoc />
        protected override void ReadValue(XmlReader reader, string name)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (string.Equals(name, ErrorKey, StringComparison.Ordinal))
            {
                reader.Read();
                ReadErrorProperty(reader);
            }
            else
            {
                base.ReadValue(reader, name);
            }
        }

        private void ReadErrorProperty(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var key = XmlConvert.DecodeName(reader.LocalName);
                var value = reader.ReadInnerXml();
                if (string.Equals(EmptyKey, key, StringComparison.Ordinal))
                {
                    key = string.Empty;
                }

                ProblemDetails.Errors.Add(key, new[] { value });
                reader.MoveToContent();
            }
        }

        /// <inheritdoc />
        public override void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            base.WriteXml(writer);

            if (ProblemDetails.Errors.Count == 0)
            {
                return;
            }

            writer.WriteStartElement(XmlConvert.EncodeLocalName(ErrorKey));

            foreach (var keyValuePair in ProblemDetails.Errors)
            {
                var key = keyValuePair.Key;
                var value = keyValuePair.Value;
                if (string.IsNullOrEmpty(key))
                {
                    key = EmptyKey;
                }

                writer.WriteStartElement(XmlConvert.EncodeLocalName(key));
                if (value != null)
                {
                    writer.WriteValue(value);
                }

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        object IUnwrappable.Unwrap(Type declaredType)
        {
            if (declaredType == null)
            {
                throw new ArgumentNullException(nameof(declaredType));
            }

            return ProblemDetails;
        }
    }
}
