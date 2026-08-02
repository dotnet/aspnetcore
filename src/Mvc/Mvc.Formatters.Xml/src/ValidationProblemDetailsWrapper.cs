// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Wrapper class for <see cref="ValidationProblemDetails"/> to enable it to be serialized by the xml formatters.
/// </summary>
[XmlRoot("problem", Namespace = "urn:ietf:rfc:7807")]
public class ValidationProblemDetailsWrapper : ProblemDetailsWrapper, IUnwrappable
{
    private const string ErrorKey = "MVC-Errors";

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationProblemDetailsWrapper"/>.
    /// </summary>
    public ValidationProblemDetailsWrapper()
        : this(new ValidationProblemDetails())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationProblemDetailsWrapper"/> for the specified
    /// <paramref name="problemDetails"/>.
    /// </summary>
    /// <param name="problemDetails">The <see cref="ProblemDetails"/>.</param>
    public ValidationProblemDetailsWrapper(ValidationProblemDetails problemDetails)
        : base(problemDetails)
    {
        ProblemDetails = problemDetails;
    }

    internal new ValidationProblemDetails ProblemDetails { get; }

    /// <inheritdoc />
    protected override void ReadValue(XmlReader reader, string name)
    {
        ArgumentNullException.ThrowIfNull(reader);

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
        ArgumentNullException.ThrowIfNull(writer);

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
        ArgumentNullException.ThrowIfNull(declaredType);

        return ProblemDetails;
    }
}
