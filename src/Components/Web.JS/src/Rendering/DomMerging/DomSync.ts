// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { applyAnyDeferredValue } from '../DomSpecialPropertyUtil';
import { synchronizeAttributes } from './AttributeSync';
import { UpdateCost, ItemList, Operation, computeEditScript } from './EditScript';

export function synchronizeDomContent(destination: CommentBoundedRange | Node, newContent: Node) {
  let destinationParent: Node;
  let nextDestinationNode: Node | null;
  let originalNodesForDiff: ItemList<Node>;

  // Figure out how to interpret the 'destination' parameter, since it can come in two very different forms
  if (destination instanceof Node) {
    destinationParent = destination;
    nextDestinationNode = destination.firstChild;
    originalNodesForDiff = destination.childNodes;
  } else {
    destinationParent = destination.startExclusive.parentNode!;
    nextDestinationNode = destination.startExclusive.nextSibling;
    originalNodesForDiff = new SiblingSubsetNodeList(destination);
  }

  // Run the diff
  const editScript = computeEditScript(
    originalNodesForDiff,
    newContent.childNodes,
    domNodeComparer);

  // Handle any common leading items
  let nextNewContentNode = newContent.firstChild; // Could be null
  for (let i = 0; i < editScript.skipCount; i++) {
    treatAsMatch(nextDestinationNode!, nextNewContentNode!);
    nextDestinationNode = nextDestinationNode!.nextSibling!;
    nextNewContentNode = nextNewContentNode!.nextSibling;
  }

  // Handle any edited region
  if (editScript.edits) {
    const edits = editScript.edits;
    const editsLength = edits.length;

    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const operation = edits[editIndex];
      switch (operation) {
        case Operation.Keep:
          treatAsMatch(nextDestinationNode!, nextNewContentNode!);
          nextDestinationNode = nextDestinationNode!.nextSibling;
          nextNewContentNode = nextNewContentNode!.nextSibling;
          break;
        case Operation.Update:
          treatAsSubstitution(nextDestinationNode!, nextNewContentNode!);
          nextDestinationNode = nextDestinationNode!.nextSibling;
          nextNewContentNode = nextNewContentNode!.nextSibling;
          break;
        case Operation.Delete:
          const nodeToRemove = nextDestinationNode!;
          nextDestinationNode = nodeToRemove.nextSibling;
          destinationParent.removeChild(nodeToRemove);
          break;
        case Operation.Insert:
          const nodeToInsert = nextNewContentNode!;
          nextNewContentNode = nodeToInsert.nextSibling;
          destinationParent.insertBefore(nodeToInsert, nextDestinationNode);
          break;
        default:
          throw new Error(`Unexpected operation: '${operation}'`);
      }
    }

    // Handle any common trailing items
    // These can only exist if there were some edits, otherwise everything would be in the set of common leading items
    const endAtNodeExclOrNull = destination instanceof Node ? null : destination.endExclusive;
    while (nextDestinationNode !== endAtNodeExclOrNull) {
      treatAsMatch(nextDestinationNode!, nextNewContentNode!);
      nextDestinationNode = nextDestinationNode!.nextSibling;
      nextNewContentNode = nextNewContentNode!.nextSibling;
    }
    if (nextNewContentNode) {
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
      applyAnyDeferredValue(destination as Element);
      synchronizeDomContent(destination as Element, source as Element);
      break;
    case Node.DOCUMENT_TYPE_NODE:
      // See comment below about doctype nodes. We leave them alone.
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
    case Node.DOCUMENT_TYPE_NODE:
      // It's invalid to insert or delete doctype, and we have no use case for doing that. So just skip such
      // nodes by saying they are always unchanged.
      return UpdateCost.None;
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
