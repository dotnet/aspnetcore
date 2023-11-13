// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const dataPermanentAttributeName = 'data-permanent';

export function isDataPermanentElement(elem: Element): boolean {
  return elem.hasAttribute(dataPermanentAttributeName);
}

export function cannotMergeDueToDataPermanentAttributes(elementA: Element, elementB: Element) {
  const dataPermanentAttributeValueA = elementA.getAttribute(dataPermanentAttributeName);
  const dataPermanentAttributeValueB = elementB.getAttribute(dataPermanentAttributeName);

  return dataPermanentAttributeValueA !== dataPermanentAttributeValueB;
}
