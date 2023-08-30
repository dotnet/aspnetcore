// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function isDataPermanentElement(elem: Element): boolean {
  return elem.hasAttribute('data-permanent');
}

export function areIncompatibleDataPermanentElements(elementA: Element, elementB: Element) {
  const isDataPermanentA = isDataPermanentElement(elementA);
  const isDataPermanentB = isDataPermanentElement(elementB);

  if (isDataPermanentA !== isDataPermanentB) {
    // A 'data permanent' element can't be merged with a 'non-data-permanent' one.
    return true;
  }

  if (isDataPermanentA && elementA.id !== elementB.id) {
    // Data permanent elements with different IDs can't be merged.
    return true;
  }

  return false;
}
