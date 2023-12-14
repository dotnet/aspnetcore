// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Wrapper class for <see cref="Mvc.ProblemDetails"/> to enable it to be serialized by the xml formatters.
/// </summary>
[XmlRoot("problem", Namespace = Namespace)]
public class ProblemDetailsWrapper : IXmlSerializable, IUnwrappable
{
    internal const string Namespace = "urn:ietf:rfc:7807";

    /// <summary>
    /// Key used to represent dictionary elements with empty keys
    /// </summary>
    protected static readonly string EmptyKey = SerializableErrorWrapper.EmptyKey;

    /// <summary>
    /// Initializes a new instance of <see cref="ProblemDetailsWrapper"/>.
    /// </summary>
    public ProblemDetailsWrapper()
        : this(new ProblemDetails())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ProblemDetailsWrapper"/>.
    /// </summary>
    public ProblemDetailsWrapper(ProblemDetails problemDetails)
    {
        ProblemDetails = problemDetails;
    }

    internal ProblemDetails ProblemDetails { get; }

    /// <inheritdoc />
    public XmlSchema? GetSchema() => null;

    /// <inheritdoc />
    public virtual void ReadXml(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.ReadStartElement();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            var key = XmlConvert.DecodeName(reader.LocalName);
            ReadValue(reader, key);

            reader.MoveToContent();
        }

        reader.ReadEndElement();
    }

    /// <summary>
    /// Reads the value for the specified <paramref name="name"/> from the <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <param name="name">The name of the node.</param>
    protected virtual void ReadValue(XmlReader reader, string name)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var value = reader.ReadInnerXml();

        switch (name)
        {
            case "detail":
                ProblemDetails.Detail = value;
                break;

            case "instance":
                ProblemDetails.Instance = value;
                break;

            case "status":
                ProblemDetails.Status = string.IsNullOrEmpty(value) ?
                    (int?)null :
                    int.Parse(value, CultureInfo.InvariantCulture);
                break;

            case "title":
                ProblemDetails.Title = value;
                break;

            case "type":
                ProblemDetails.Type = value;
                break;

            default:
                if (string.Equals(name, EmptyKey, StringComparison.Ordinal))
                {
                    name = string.Empty;
                }

                ProblemDetails.Extensions.Add(name, value);
                break;
        }
    }

    /// <inheritdoc />
    public virtual void WriteXml(XmlWriter writer)
    {
        if (!string.IsNullOrEmpty(ProblemDetails.Detail))
        {
            writer.WriteElementString(
                XmlConvert.EncodeLocalName("detail"),
                ProblemDetails.Detail);
        }

        if (!string.IsNullOrEmpty(ProblemDetails.Instance))
        {
            writer.WriteElementString(
                XmlConvert.EncodeLocalName("instance"),
                ProblemDetails.Instance);
        }

        if (ProblemDetails.Status.HasValue)
        {
            writer.WriteStartElement(XmlConvert.EncodeLocalName("status"));
            writer.WriteValue(ProblemDetails.Status.Value);
            writer.WriteEndElement();
        }

        if (!string.IsNullOrEmpty(ProblemDetails.Title))
        {
            writer.WriteElementString(
                XmlConvert.EncodeLocalName("title"),
                ProblemDetails.Title);
        }

        if (!string.IsNullOrEmpty(ProblemDetails.Type))
        {
            writer.WriteElementString(
                XmlConvert.EncodeLocalName("type"),
                ProblemDetails.Type);
        }

        foreach (var keyValuePair in ProblemDetails.Extensions)
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

    object IUnwrappable.Unwrap(Type declaredType)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        return ProblemDetails;
    }
}
