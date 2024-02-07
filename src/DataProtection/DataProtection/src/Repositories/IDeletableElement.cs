// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

/// <summary>
/// Represents an XML element in an <see cref="IXmlRepository"/> that can be deleted.
/// </summary>
public interface IDeletableElement
{
    /// <summary>The XML element.</summary>
    public XElement Element { get; }
    /// <summary>Set to true if the element should be deleted.</summary>
    public bool ShouldDelete { get; set; }
}
