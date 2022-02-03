// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

/// <summary>
/// Contains methods which are used by Xml input formatters.
/// </summary>
internal static class FormattingUtilities
{
    public const int DefaultMaxDepth = 32;
    public static readonly XsdDataContractExporter XsdDataContractExporter = new XsdDataContractExporter();

    /// <summary>
    /// Gets the default Reader Quotas for XmlReader.
    /// </summary>
    /// <returns>XmlReaderQuotas with default values</returns>
    public static XmlDictionaryReaderQuotas GetDefaultXmlReaderQuotas()
    {
        return new XmlDictionaryReaderQuotas()
        {
            MaxArrayLength = int.MaxValue,
            MaxBytesPerRead = int.MaxValue,
            MaxDepth = DefaultMaxDepth,
            MaxNameTableCharCount = int.MaxValue,
            MaxStringContentLength = int.MaxValue
        };
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
}
