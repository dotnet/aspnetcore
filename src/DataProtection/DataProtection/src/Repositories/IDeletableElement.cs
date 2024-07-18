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
    /// <summary>Elements are deleted in increasing DeletionOrder.  <c>null</c> means "don't delete".</summary>
    public int? DeletionOrder { get; set; }
}
