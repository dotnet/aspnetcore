// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function synchronizeAttributes(destination: Element, source: Element) {
  const destAttrs = destination.attributes;
  const sourceAttrs = source.attributes;

  // Skip most of the work in the common case where all attributes are unchanged and are even still in the same order
  if (!attributeSetsAreIdentical(destAttrs, sourceAttrs)) {
    // Certain element types may have special rules about how to update their attributes,
    // or might require us to synchronize DOM properties as well as attributes
    if (destination instanceof HTMLLinkElement || destination instanceof HTMLScriptElement) {
      destination.integrity = (source as HTMLLinkElement | HTMLScriptElement).integrity;
    }

    // Now do generic unordered attribute synchronization
    const remainingDestAttrs = new Map<string, Attr>();
    for (const destAttr of destination.attributes as any) {
      remainingDestAttrs.set(destAttr.name, destAttr);
    }

    for (const sourceAttr of source.attributes as any as Attr[]) {
      const existingDestAttr = sourceAttr.namespaceURI
        ? destination.getAttributeNodeNS(sourceAttr.namespaceURI, sourceAttr.localName)
        : destination.getAttributeNode(sourceAttr.name);
      if (existingDestAttr) {
        if (existingDestAttr.value !== sourceAttr.value) {
          // Update
          applyAttribute(destination, sourceAttr);
        }

        remainingDestAttrs.delete(existingDestAttr.name);
      } else {
        // Insert
        applyAttribute(destination, sourceAttr);
      }
    }

    for (const attrToDelete of remainingDestAttrs.values()) {
      // Delete
      removeAttribute(destination, attrToDelete);
    }
  }
}

function attributeSetsAreIdentical(destAttrs: NamedNodeMap, sourceAttrs: NamedNodeMap): boolean {
  const destAttrsLength = destAttrs.length;
  if (destAttrsLength !== sourceAttrs.length) {
    return false;
  }

  for (let i = 0; i < destAttrsLength; i++) {
    const sourceAttr = sourceAttrs.item(i)!;
    const destAttr = destAttrs.item(i)!;
    if (sourceAttr.name !== destAttr.name || sourceAttr.value !== destAttr.value) {
      return false;
    }
  }

  return true;
}

function applyAttribute(element: Element, attr: Attr) {
  if (attr.namespaceURI) {
    element.setAttributeNS(attr.namespaceURI, attr.name, attr.value);
  } else {
    element.setAttribute(attr.name, attr.value);
  }
}

function removeAttribute(element: Element, attr: Attr) {
  if (attr.namespaceURI) {
    element.removeAttributeNS(attr.namespaceURI, attr.localName);
  } else {
    element.removeAttribute(attr.name);
  }
}
