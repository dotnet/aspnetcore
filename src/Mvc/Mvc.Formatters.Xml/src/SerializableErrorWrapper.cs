// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Wrapper class for <see cref="SerializableError"/> to enable it to be serialized by the xml formatters.
/// </summary>
[XmlRoot("Error")]
public sealed class SerializableErrorWrapper : IXmlSerializable, IUnwrappable
{
    // Element name used when ModelStateEntry's Key is empty. Dash in element name should avoid collisions with
    // other ModelState entries because the character is not legal in an expression name.
    internal const string EmptyKey = "MVC-Empty";

    /// <summary>
    /// Initializes a new <see cref="SerializableErrorWrapper"/>
    /// </summary>
    // Note: XmlSerializer requires to have default constructor
    public SerializableErrorWrapper()
    {
        SerializableError = new SerializableError();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializableErrorWrapper"/> class.
    /// </summary>
    /// <param name="error">The <see cref="SerializableError"/> object that needs to be wrapped.</param>
    public SerializableErrorWrapper(SerializableError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        SerializableError = error;
    }

    /// <summary>
    /// Gets the wrapped object which is serialized/deserialized into XML
    /// representation.
    /// </summary>
    public SerializableError SerializableError { get; }

    /// <inheritdoc />
    public XmlSchema? GetSchema()
    {
        return null;
    }

    /// <summary>
    /// Generates a <see cref="SerializableError"/> object from its XML representation.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/> stream from which the object is deserialized.</param>
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
            if (string.Equals(EmptyKey, key, StringComparison.Ordinal))
            {
                key = string.Empty;
            }

            SerializableError.Add(key, value);
            reader.MoveToContent();
        }

        reader.ReadEndElement();
    }

    /// <summary>
    /// Converts the wrapped <see cref="SerializableError"/> object into its XML representation.
    /// </summary>
    /// <param name="writer">The <see cref="XmlWriter"/> stream to which the object is serialized.</param>
    public void WriteXml(XmlWriter writer)
    {
        foreach (var keyValuePair in SerializableError)
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
    }

    /// <inheritdoc />
    public object Unwrap(Type declaredType)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        return SerializableError;
    }
}
