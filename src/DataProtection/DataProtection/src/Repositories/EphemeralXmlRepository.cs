// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.DataProtection.Repositories;

/// <summary>
/// An ephemeral XML repository backed by process memory. This class must not be used for
/// anything other than dev scenarios as the keys will not be persisted to storage.
/// </summary>
internal sealed class EphemeralXmlRepository : IXmlRepository
{
    private readonly List<XElement> _storedElements = new List<XElement>();

    public EphemeralXmlRepository(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<EphemeralXmlRepository>();
        logger.UsingInmemoryRepository();
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        // force complete enumeration under lock for thread safety
        lock (_storedElements)
        {
            return GetAllElementsCore().ToList().AsReadOnly();
        }
    }

    private IEnumerable<XElement> GetAllElementsCore()
    {
        // this method must be called under lock
        foreach (XElement element in _storedElements)
        {
            yield return new XElement(element); // makes a deep copy so caller doesn't inadvertently modify it
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullThrowHelper.ThrowIfNull(element);

        var cloned = new XElement(element); // makes a deep copy so caller doesn't inadvertently modify it

        // under lock for thread safety
        lock (_storedElements)
        {
            _storedElements.Add(cloned);
        }
    }
}
