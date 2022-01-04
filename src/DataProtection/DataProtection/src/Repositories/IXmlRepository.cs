// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

/// <summary>
/// The basic interface for storing and retrieving XML elements.
/// </summary>
public interface IXmlRepository
{
    /// <summary>
    /// Gets all top-level XML elements in the repository.
    /// </summary>
    /// <remarks>
    /// All top-level elements in the repository.
    /// </remarks>
    IReadOnlyCollection<XElement> GetAllElements();

    /// <summary>
    /// Adds a top-level XML element to the repository.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <param name="friendlyName">An optional name to be associated with the XML element.
    /// For instance, if this repository stores XML files on disk, the friendly name may
    /// be used as part of the file name. Repository implementations are not required to
    /// observe this parameter even if it has been provided by the caller.</param>
    /// <remarks>
    /// The 'friendlyName' parameter must be unique if specified. For instance, it could
    /// be the id of the key being stored.
    /// </remarks>
    void StoreElement(XElement element, string friendlyName);
}
