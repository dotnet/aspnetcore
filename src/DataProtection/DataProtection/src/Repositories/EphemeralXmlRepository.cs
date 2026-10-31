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
internal sealed class EphemeralXmlRepository : IDeletableXmlRepository
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

    /// <inheritdoc/>
    public bool DeleteElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements)
    {
        ArgumentNullThrowHelper.ThrowIfNull(chooseElements);

        var deletableElements = new List<DeletableElement>();

        lock (_storedElements)
        {
            foreach (var storedElement in _storedElements)
            {
                // Make a deep copy so caller doesn't inadvertently modify it.
                deletableElements.Add(new DeletableElement(storedElement, new XElement(storedElement)));
            }
        }

        chooseElements(deletableElements);

        var elementsToDelete = deletableElements
            .Where(e => e.DeletionOrder.HasValue)
            .OrderBy(e => e.DeletionOrder.GetValueOrDefault());

        lock (_storedElements)
        {
            foreach (var deletableElement in elementsToDelete)
            {
                var storedElement = deletableElement.StoredElement;
                var index = _storedElements.FindIndex(e => ReferenceEquals(e, storedElement));
                if (index >= 0) // Might not find it if the collection has changed since we started.
                {
                    // It would be more efficient to remove the larger indices first, but deletion order
                    // is important for correctness.
                    _storedElements.RemoveAt(index);
                }
            }
        }

        return true;
    }

    private sealed class DeletableElement : IDeletableElement
    {
        public DeletableElement(XElement storedElement, XElement element)
        {
            StoredElement = storedElement;
            Element = element;
        }

        /// <inheritdoc/>
        public XElement Element { get; }

        /// <summary>The <see cref="XElement"/> from which <see cref="Element"/> was cloned.</summary>
        public XElement StoredElement { get; }

        /// <inheritdoc/>
        public int? DeletionOrder { get; set; }
    }
}
