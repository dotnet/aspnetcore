import { UpdateCost, ItemList, Operation, computeEditScript } from './EditScript';

export function synchronizeDomContent(destination: CommentBoundedRange, source: DocumentFragment) {
  const destinationParent = destination.startExclusive.parentNode!;

  const editScript = computeEditScript(
    new SiblingSubsetNodeList(destination),
    source.childNodes,
    domNodeComparer);

  let nextDestinationNode = destination.startExclusive.nextSibling!; // Never null because it walks a range that ends with the end comment
  let nextSourceNode = source.firstChild; // Could be null

  // Handle any common leading items
  for (let i = 0; i < editScript.skipCount; i++) {
    treatAsMatch(nextDestinationNode, nextSourceNode!);
    nextDestinationNode = nextDestinationNode.nextSibling!;
    nextSourceNode = nextSourceNode!.nextSibling;
  }

  // Handle any edited region
  if (editScript.edits) {
    const edits = editScript.edits;
    const editsLength = edits.length;

    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const operation = edits[editIndex];
      switch (operation) {
        case Operation.Keep:
          treatAsMatch(nextDestinationNode, nextSourceNode!);
          nextDestinationNode = nextDestinationNode.nextSibling!;
          nextSourceNode = nextSourceNode!.nextSibling;
          break;
        case Operation.Update:
          treatAsSubstitution(nextDestinationNode, nextSourceNode!);
          nextDestinationNode = nextDestinationNode.nextSibling!;
          nextSourceNode = nextSourceNode!.nextSibling;
          break;
        case Operation.Delete:
          const nodeToRemove = nextDestinationNode;
          nextDestinationNode = nodeToRemove.nextSibling!;
          destinationParent.removeChild(nodeToRemove);
          break;
        case Operation.Insert:
          const nodeToInsert = nextSourceNode!;
          nextSourceNode = nodeToInsert.nextSibling;
          destinationParent.insertBefore(nodeToInsert, nextDestinationNode);
          break;
        default:
          throw new Error(`Unexpected operation: '${operation}'`);
      }
    }

    // Handle any common trailing items
    // These can only exist if there were some edits, otherwise everything would be in the set of common leading items
    while (nextDestinationNode !== destination.endExclusive) {
      treatAsMatch(nextDestinationNode, nextSourceNode!);
      nextDestinationNode = nextDestinationNode.nextSibling!;
      nextSourceNode = nextSourceNode!.nextSibling;
    }
    if (nextSourceNode) {
      // Should never be possible, as it would imply a bug in the edit script calculation, or possibly an unsupported
      // scenario like a DOM mutation observer modifying the destination nodes while we are working on them
      throw new Error('Updating the DOM failed because the sets of trailing nodes had inconsistent lengths.');
    }
  }
}

function treatAsMatch(destination: Node, source: Node) {
  switch (destination.nodeType) {
    case Node.TEXT_NODE:
    case Node.COMMENT_NODE:
      break;
    case Node.ELEMENT_NODE:
      synchronizeAttributes(destination as Element, source as Element);
      // TODO: Recurse into descendants
      break;
    default:
      throw new Error(`Not implemented: matching nodes of type ${destination.nodeType}`);
  }
}

function treatAsSubstitution(destination: Node, source: Node) {
  switch (destination.nodeType) {
    case Node.TEXT_NODE:
    case Node.COMMENT_NODE:
      (destination as Text).textContent = (source as Text).textContent;
      break;
    default:
      throw new Error(`Not implemented: substituting nodes of type ${destination.nodeType}`);
  }
}

function domNodeComparer(a: Node, b: Node): UpdateCost {
  if (a.nodeType !== b.nodeType) {
    return UpdateCost.Infinite;
  }

  switch (a.nodeType) {
    case Node.TEXT_NODE:
    case Node.COMMENT_NODE:
      // We're willing to update text and comment nodes in place, but treat the update operation as being
      // as costly as an insertion or deletion
      return a.textContent === b.textContent ? UpdateCost.None : UpdateCost.Some;
    case Node.ELEMENT_NODE:
      // For elements, we're only doing a shallow comparison and don't know if attributes/descendants are different.
      // We never 'update' one element type into another. We regard the update cost for same-type elements as zero because
      // then the 'find common prefix/suffix' optimization can include elements in those prefixes/suffixes.
      // TODO: If we want to support some way to force matching/nonmatching based on @key, we can add logic here
      //       to return UpdateCost.Infinite if either has a key but they don't match. This will prevent unwanted retention.
      //       For the converse (forcing retention, even if that means reordering), we could post-process the list of
      //       inserts/deletes to find matches based on key to treat those pairs as 'move' operations.
      return (a as Element).tagName === (b as Element).tagName ? UpdateCost.None : UpdateCost.Infinite;
    default:
      // For anything else we know nothing, so the risk-averse choice is to say we can't retain or update the old value
      return UpdateCost.Infinite;
  }
}

export interface CommentBoundedRange {
  startExclusive: Comment,
  endExclusive: Comment,
}

class SiblingSubsetNodeList implements ItemList<Node> {
  private readonly siblings: NodeList;
  private readonly startIndex: number;
  private readonly endIndexExcl: number;

  readonly length: number;

  item(index: number): Node | null {
    return this.siblings.item(this.startIndex + index);
  }

  forEach(callbackfn: (value: Node, key: number, parent: ItemList<Node>) => void, thisArg?: any): void {
    for (let i = 0; i < this.length; i++) {
      callbackfn.call(thisArg, this.item(i)!, i, this);
    }
  }

  constructor(range: CommentBoundedRange) {
    if (!range.startExclusive.parentNode || range.startExclusive.parentNode !== range.endExclusive.parentNode) {
      throw new Error('Invalid CommentBoundedRange. The start and end markers have no common parent.');
    }

    this.siblings = range.startExclusive.parentNode!.childNodes;
    this.startIndex = Array.prototype.indexOf.call(this.siblings, range.startExclusive) + 1;
    this.endIndexExcl = Array.prototype.indexOf.call(this.siblings, range.endExclusive);
    this.length = this.endIndexExcl - this.startIndex;
  }
}

function synchronizeAttributes(destination: Element, source: Element) {
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
