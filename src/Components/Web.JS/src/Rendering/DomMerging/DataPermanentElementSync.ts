// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const dataPermanentAttributeName = 'data-permanent';
const dynamicallyPermanentElements = new WeakSet<Element>();

export function markAsDataPermanentElement(element: Element): void {
  dynamicallyPermanentElements.add(element);
}

export function isDynamicallyPermanentElement(elem: Element): boolean {
  return dynamicallyPermanentElements.has(elem);
}

export function isDataPermanentElement(elem: Element): boolean {
  return elem.hasAttribute(dataPermanentAttributeName) || isDynamicallyPermanentElement(elem);
}

export function cannotMergeDueToDataPermanentAttributes(elementA: Element, elementB: Element) {
  if (isDynamicallyPermanentElement(elementA)) {
    return elementA.id !== elementB.id;
  }

  const dataPermanentAttributeValueA = elementA.getAttribute(dataPermanentAttributeName);
  const dataPermanentAttributeValueB = elementB.getAttribute(dataPermanentAttributeName);

  return dataPermanentAttributeValueA !== dataPermanentAttributeValueB;
}
