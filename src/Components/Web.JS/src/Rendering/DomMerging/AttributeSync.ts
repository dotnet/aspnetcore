export function synchronizeAttributes(destination: Element, source: Element) {
  const destAttrs = destination.attributes;
  const sourceAttrs = source.attributes;

  // Optimize for the common case where all attributes are unchanged and are even still in the same order
  const destAttrsLength = destAttrs.length;
  if (destAttrsLength === destAttrs.length) {
    let hasDifference = false;
    for (let i = 0; i < destAttrsLength; i++) {
      const sourceAttr = sourceAttrs.item(i)!;
      const destAttr = destAttrs.item(i)!;
      if (sourceAttr.name !== destAttr.name || sourceAttr.value !== destAttr.value) {
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
      if (existingDestAttr.value !== sourceAttr.value) {
        // Update
        existingDestAttr.value = sourceAttr.value;
      }

      remainingDestAttrs.delete(existingDestAttr.name);
    } else {
      // Insert
      if (sourceAttr.namespaceURI) {
        destination.setAttributeNS(sourceAttr.namespaceURI, sourceAttr.name, sourceAttr.value);
      } else {
        destination.setAttribute(sourceAttr.name, sourceAttr.value);
      }
    }
  }

  for (const attrToDelete of remainingDestAttrs.values()) {
    // Delete
    if (attrToDelete.namespaceURI) {
      destination.removeAttributeNS(attrToDelete.namespaceURI, attrToDelete.localName);
    } else {
      destination.removeAttribute(attrToDelete.name);
    }
  }
}
