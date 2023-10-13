// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Microsoft.AspNetCore.Internal;

internal sealed class EphemeralXmlRepository : IXmlRepository
{
	private readonly List<XElement> _storedElements = new List<XElement>();

	public EphemeralXmlRepository()
	{
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
		var cloned = new XElement(element); // makes a deep copy so caller doesn't inadvertently modify it

		// under lock for thread safety
		lock (_storedElements)
		{
			_storedElements.Add(cloned);
		}
	}
}
