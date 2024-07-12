// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

/// <summary>
/// The basic interface for storing and retrieving XML elements.
/// </summary>
public interface IXmlRepositoryWithDeletion : IXmlRepository
{
    /// <summary>
    /// Removes selected elements from the repository.
    /// </summary>
    /// <param name="chooseElements">
    /// A snapshot of the elements in this repository.
    /// For each, set <see cref="IDeletableElement.DeletionOrder"/> to a non-<c>null</c> value if it should be deleted.
    /// Elements are deleted in increasing order.  If any deletion fails, the remaining deletions *MUST* be skipped.
    /// </param>
    /// <returns>
    /// True if all deletions succeeded.
    /// </returns>
    bool RemoveElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements);
}
