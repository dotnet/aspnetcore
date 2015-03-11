// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.Repositories
{
    /// <summary>
    /// An ephemeral XML repository backed by process memory. This class must not be used for
    /// anything other than dev scenarios as the keys will not be persisted to storage.
    /// </summary>
    internal class EphemeralXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _storedElements = new List<XElement>();

        public EphemeralXmlRepository(IServiceProvider services)
        {
            var logger = services?.GetLogger<EphemeralXmlRepository>();
            if (logger.IsWarningLevelEnabled())
            {
                logger.LogWarning("Using an in-memory repository. Keys will not be persisted to storage.");
            }
        }

        public virtual IReadOnlyCollection<XElement> GetAllElements()
        {
            // force complete enumeration under lock to avoid races
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

        public virtual void StoreElement([NotNull] XElement element, string friendlyName)
        {
            XElement cloned = new XElement(element); // makes a deep copy so caller doesn't inadvertently modify it

            // under lock to avoid races
            lock (_storedElements)
            {
                _storedElements.Add(cloned);
            }
        }
    }
}
