// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readSpecialPropertyOrAttributeValue, tryApplySpecialProperty } from '../DomSpecialPropertyUtil';

export function synchronizeAttributes(destination: Element, source: Element) {
  const destAttrs = destination.attributes;
  const sourceAttrs = source.attributes;

  // Optimize for the common case where all attributes are unchanged and are even still in the same order
  const destAttrsLength = destAttrs.length;
  if (destAttrsLength === sourceAttrs.length) {
    let hasDifference = false;
    for (let i = 0; i < destAttrsLength; i++) {
      const sourceAttr = sourceAttrs.item(i)!;
      const destAttr = destAttrs.item(i)!;
      if (sourceAttr.name !== destAttr.name || readSpecialPropertyOrAttributeValue(sourceAttr) !== readSpecialPropertyOrAttributeValue(destAttr)) {
        hasDifference = true;
        break;
      }
    }

    if (!hasDifference) {
      return;
    }
  }

  // There's some difference
  const remainingDestAttrs = new Map<string, Attr>();
  for (const destAttr of destination.attributes as any) {
    remainingDestAttrs.set(destAttr.name, destAttr);
  }

  for (const sourceAttr of source.attributes as any as Attr[]) {
    const existingDestAttr = sourceAttr.namespaceURI
      ? destination.getAttributeNodeNS(sourceAttr.namespaceURI, sourceAttr.localName)
      : destination.getAttributeNode(sourceAttr.name);
    if (existingDestAttr) {
      if (readSpecialPropertyOrAttributeValue(existingDestAttr) !== readSpecialPropertyOrAttributeValue(sourceAttr)) {
        // Update
        applyAttributeOrProperty(destination, sourceAttr);
      }

      remainingDestAttrs.delete(existingDestAttr.name);
    } else {
      // Insert
      applyAttributeOrProperty(destination, sourceAttr);
    }
  }

  for (const attrToDelete of remainingDestAttrs.values()) {
    // Delete
    removeAttributeOrProperty(destination, attrToDelete);
  }
}

function applyAttributeOrProperty(element: Element, attr: Attr) {
  // If we need to assign a special property on the element, do so
  tryApplySpecialProperty(element, attr.name, attr.value)

  // Either way, also update the attribute
  if (attr.namespaceURI) {
    element.setAttributeNS(attr.namespaceURI, attr.name, attr.value);
  } else {
    element.setAttribute(attr.name, attr.value);
  }
}

function removeAttributeOrProperty(element: Element, attr: Attr) {
  // If we need to null out a special property on the element, do so
  tryApplySpecialProperty(element, attr.name, null);

  // Either way, also remove the attribute
  if (attr.namespaceURI) {
    element.removeAttributeNS(attr.namespaceURI, attr.localName);
  } else {
    element.removeAttribute(attr.name);
  }
}
