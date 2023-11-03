// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AutoComponentDescriptor, ComponentDescriptor, ServerComponentDescriptor, WebAssemblyComponentDescriptor, canMergeDescriptors, discoverComponents, mergeDescriptors } from '../../Services/ComponentDescriptorDiscovery';
import { isInteractiveRootComponentElement } from '../BrowserRenderer';
import { applyAnyDeferredValue } from '../DomSpecialPropertyUtil';
import { LogicalElement, getLogicalChildrenArray, getLogicalNextSibling, getLogicalParent, getLogicalRootDescriptor, insertLogicalChild, insertLogicalChildBefore, isLogicalElement, toLogicalElement, toLogicalRootCommentElement } from '../LogicalElements';
import { synchronizeAttributes } from './AttributeSync';
import { cannotMergeDueToDataPermanentAttributes, isDataPermanentElement } from './DataPermanentElementSync';
import { UpdateCost, ItemList, Operation, computeEditScript } from './EditScript';

let descriptorHandler: DescriptorHandler | null = null;

export interface DescriptorHandler {
  registerComponent(descriptor: ComponentDescriptor): void;
}

export function attachComponentDescriptorHandler(handler: DescriptorHandler) {
  descriptorHandler = handler;
}

export function registerAllComponentDescriptors(root: Node) {
  const descriptors = upgradeComponentCommentsToLogicalRootComments(root);

  for (const descriptor of descriptors) {
    descriptorHandler?.registerComponent(descriptor);
  }
}

export { preprocessAndSynchronizeDomContent as synchronizeDomContent };

function preprocessAndSynchronizeDomContent(destination: CommentBoundedRange | Node, newContent: Node) {
  // Start by recursively identifying component markers in the new content
  // and converting them into logical elements so they correctly participate
  // in logical element synchronization
  upgradeComponentCommentsToLogicalRootComments(newContent);

  // Then, synchronize the preprocessed DOM content
  synchronizeDomContentCore(destination, newContent);
}

function synchronizeDomContentCore(destination: CommentBoundedRange | Node, newContent: Node) {
  // Determine the destination's parent node, i.e., the node containing the children
  // we intend to synchronize. This might sometimes be a logical parent.
  let destinationParent: Node;
  if (destination instanceof Node) {
    destinationParent = destination;
  } else {
    destinationParent =
      getLogicalParent(destination.startExclusive as unknown as LogicalElement) as unknown as Node ??
      destination.startExclusive.parentNode!;
  }

  // If only one out of the destination and new content is a logical element, we normalize
  // the other to also be a logical element
  const isSynchronizingLogicalElements = isLogicalElement(destinationParent) || isLogicalElement(newContent);
  if (isSynchronizingLogicalElements) {
    toLogicalElement(destinationParent, /* allowExistingContents */ true);
    toLogicalElement(newContent, /* allowExistingContents */ true);
  }

  // Create abstract lists of child nodes
  let originalNodesForDiff: ItemList<Node>;
  let newNodesForDiff: ItemList<Node>;
  if (isSynchronizingLogicalElements) {
    originalNodesForDiff = new LogicalElementNodeList(destinationParent as unknown as LogicalElement);
    newNodesForDiff = new LogicalElementNodeList(newContent as unknown as LogicalElement);
  } else {
    originalNodesForDiff = destinationParent.childNodes;
    newNodesForDiff = newContent.childNodes;
  }

  // If the destination is a comment bounded range, limit the node list to a subset defined
  // by that range
  if (!(destination instanceof Node)) {
    originalNodesForDiff = new SiblingSubsetNodeList(originalNodesForDiff, destination);
  }

  // Run the diff
  const editScript = computeEditScript(
    originalNodesForDiff,
    newNodesForDiff,
    domNodeComparer
  );

  let destinationWalker: EditWalker;
  let sourceWalker: EditWalker;
  if (isSynchronizingLogicalElements) {
    destinationWalker = new LogicalElementEditWalker(originalNodesForDiff.item(0) as unknown as LogicalElement);
    sourceWalker = new LogicalElementEditWalker(newNodesForDiff.item(0) as unknown as LogicalElement);
  } else {
    destinationWalker = new DomNodeEditWalker(originalNodesForDiff.item(0)!);
    sourceWalker = new DomNodeEditWalker(newNodesForDiff.item(0)!);
  }

  // Handle any common leading items
  for (let i = 0; i < editScript.skipCount; i++) {
    treatAsMatch(destinationWalker.current, sourceWalker.current);
    destinationWalker.advance();
    sourceWalker.advance();
  }

  // Handle any edited region
  if (editScript.edits) {
    const edits = editScript.edits;
    const editsLength = edits.length;

    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const operation = edits[editIndex];
      switch (operation) {
        case Operation.Keep: {
          treatAsMatch(destinationWalker.current, sourceWalker.current);
          destinationWalker.advance();
          sourceWalker.advance();
          break;
        }
        case Operation.Update: {
          treatAsSubstitution(destinationWalker.current, sourceWalker.current);
          destinationWalker.advance();
          sourceWalker.advance();
          break;
        }
        case Operation.Delete: {
          const nodeToRemove = destinationWalker.current;
          destinationWalker.advance();
          treatAsDeletion(nodeToRemove, destinationParent);
          break;
        }
        case Operation.Insert: {
          const nodeToInsert = sourceWalker.current;
          sourceWalker.advance();
          treatAsInsertion(nodeToInsert, destinationWalker.current, destinationParent);
          break;
        }
        default:
          throw new Error(`Unexpected operation: '${operation}'`);
      }
    }

    // Handle any common trailing items
    // These can only exist if there were some edits, otherwise everything would be in the set of common leading items
    const endAtNodeExclOrNull = destination instanceof Node ? null : destination.endExclusive;
    while (destinationWalker.current !== endAtNodeExclOrNull) {
      treatAsMatch(destinationWalker.current, sourceWalker.current);
      destinationWalker.advance();
      sourceWalker.advance();
    }
    if (sourceWalker.current) {
      // Should never be possible, as it would imply a bug in the edit script calculation, or possibly an unsupported
      // scenario like a DOM mutation observer modifying the destination nodes while we are working on them
      throw new Error('Updating the DOM failed because the sets of trailing nodes had inconsistent lengths.');
    }
  }
}

function treatAsMatch(destination: Node, source: Node) {
  switch (destination.nodeType) {
    case Node.TEXT_NODE:
      break;
    case Node.COMMENT_NODE: {
      const destinationAsLogicalElement = destination as unknown as LogicalElement;
      const sourceAsLogicalElement = source as unknown as LogicalElement;
      const destinationRootDescriptor = getLogicalRootDescriptor(destinationAsLogicalElement);
      const sourceRootDescriptor = getLogicalRootDescriptor(sourceAsLogicalElement);

      if (!destinationRootDescriptor !== !sourceRootDescriptor) {
        throw new Error('Not supported: merging component comment nodes with non-component comment nodes');
      }

      if (destinationRootDescriptor) {
        // Update the existing descriptor with hte new descriptor's data
        mergeDescriptors(destinationRootDescriptor, sourceRootDescriptor);

        const isDestinationInteractive = isInteractiveRootComponentElement(destinationAsLogicalElement);
        if (isDestinationInteractive) {
          // Don't sync DOM content for already-interactive components becuase their content is managed
          // by the renderer.
        } else {
          synchronizeDomContentCore(destination, source);
        }
      }
      break;
    }
    case Node.ELEMENT_NODE: {
      const editableElementValue = getEditableElementValue(source as Element);
      synchronizeAttributes(destination as Element, source as Element);
      applyAnyDeferredValue(destination as Element);

      if (isDataPermanentElement(destination as Element)) {
        // The destination element's content should be retained, so we avoid recursing into it.
      } else {
        synchronizeDomContentCore(destination as Element, source as Element);
      }

      // This is a much simpler alternative to the deferred-value-assignment logic we use in interactive rendering.
      // Because this sync algorithm goes depth-first, we know all the attributes and descendants are fully in sync
      // by now, so setting any "special value" property is just a matter of assigning it right now (we don't have
      // to be concerned that it's invalid because it doesn't correspond to an <option> child or a min/max attribute).
      if (editableElementValue !== null) {
        ensureEditableValueSynchronized(destination as Element, editableElementValue);
      }
      break;
    }
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

function treatAsDeletion(nodeToDelete: Node, parentNode: Node) {
  if (isLogicalElement(parentNode)) {
    // It's not safe to call 'removeLogicalChild' here because it recursively removes
    // logical descendants from their parents, and that can potentially interfere with
    // renderer-managed DOM. Instead, we insert the logical element into a new document
    // fragment, which allows the renderer to continue applying render batches until
    // related components get disposed.
    const docFrag = toLogicalElement(document.createDocumentFragment());
    insertLogicalChild(nodeToDelete, docFrag, 0);
  } else {
    parentNode.removeChild(nodeToDelete);
  }
}

function treatAsInsertion(nodeToInsert: Node, nextNode: Node | null, parentNode: Node) {
  if (isLogicalElement(parentNode)) {
    insertLogicalChildBefore(nodeToInsert, parentNode as unknown as LogicalElement, nextNode as unknown as LogicalElement);
  } else {
    // If the parent node is not a logical element, that means
    // the node we're inserting is either a root logical element, or not a logical
    // element at all. In either case, it's safe to treat the node we're inserting
    // as a single node because root logical nodes cannot be component comments.
    parentNode.insertBefore(nodeToInsert, nextNode);
  }

  // Find and register descriptors in new content
  const iterator = document.createNodeIterator(nodeToInsert, NodeFilter.SHOW_COMMENT);
  while (iterator.nextNode()) {
    const logicalRootDescriptor = getLogicalRootDescriptor(iterator.referenceNode as unknown as LogicalElement);
    if (logicalRootDescriptor) {
      descriptorHandler?.registerComponent(logicalRootDescriptor);
    }
  }
}

function domNodeComparer(a: Node, b: Node): UpdateCost {
  if (a.nodeType !== b.nodeType) {
    return UpdateCost.Infinite;
  }

  if (isLogicalElement(a) !== isLogicalElement(b)) {
    // We cannot merge logical elements with non-logical elements.
    return UpdateCost.Infinite;
  }

  switch (a.nodeType) {
    case Node.TEXT_NODE:
      // We're willing to update text nodes in place, but treat the update operation as being
      // as costly as an insertion or deletion
      return a.textContent === b.textContent ? UpdateCost.None : UpdateCost.Some;
    case Node.COMMENT_NODE: {
      const rootDescriptorA = getLogicalRootDescriptor(a as unknown as LogicalElement);
      const rootDescriptorB = getLogicalRootDescriptor(b as unknown as LogicalElement);

      if (rootDescriptorA || rootDescriptorB) {
        // If either node represents a root component comment, they must both be components of with matching keys.
        // We will update a component with a non-component or a component with a different key.
        return rootDescriptorA && rootDescriptorB && canMergeDescriptors(rootDescriptorA, rootDescriptorB)
          ? UpdateCost.None
          : UpdateCost.Infinite;
      } else {
        // We're willing to update non-component comment nodes in place, but treat the update operation as being
        // as costly as an insertion or deletion
        return a.textContent === b.textContent ? UpdateCost.None : UpdateCost.Some;
      }
    }
    case Node.ELEMENT_NODE:
      // For elements, we're only doing a shallow comparison and don't know if attributes/descendants are different.
      // We never 'update' one element type into another. We regard the update cost for same-type elements as zero because
      // then the 'find common prefix/suffix' optimization can include elements in those prefixes/suffixes.
      // TODO: If we want to support some way to force matching/nonmatching based on @key, we can add logic here
      //       to return UpdateCost.Infinite if either has a key but they don't match. This will prevent unwanted retention.
      //       For the converse (forcing retention, even if that means reordering), we could post-process the list of
      //       inserts/deletes to find matches based on key to treat those pairs as 'move' operations.
      if ((a as Element).tagName !== (b as Element).tagName) {
        return UpdateCost.Infinite;
      }

      // The two elements must have matching 'data-permanent' attribute values for them to be merged. If they don't match, either:
      // [1] We're comparing a data-permanent element to a non-data-permanent one.
      // [2] We're comparing elements that represent two different data-permanent containers.
      if (cannotMergeDueToDataPermanentAttributes(a as Element, b as Element)) {
        return UpdateCost.Infinite;
      }

      return UpdateCost.None;
    case Node.DOCUMENT_TYPE_NODE:
      // It's invalid to insert or delete doctype, and we have no use case for doing that. So just skip such
      // nodes by saying they are always unchanged.
      return UpdateCost.None;
    default:
      // For anything else we know nothing, so the risk-averse choice is to say we can't retain or update the old value
      return UpdateCost.Infinite;
  }
}

function upgradeComponentCommentsToLogicalRootComments(root: Node): ComponentDescriptor[] {
  const serverDescriptors = discoverComponents(root, 'server') as ServerComponentDescriptor[];
  const webAssemblyDescriptors = discoverComponents(root, 'webassembly') as WebAssemblyComponentDescriptor[];
  const autoDescriptors = discoverComponents(root, 'auto') as AutoComponentDescriptor[];
  const allDescriptors: ComponentDescriptor[] = [];

  for (const descriptor of [
    ...serverDescriptors,
    ...webAssemblyDescriptors,
    ...autoDescriptors,
  ]) {
    const existingDescriptor = getLogicalRootDescriptor(descriptor.start as unknown as LogicalElement);
    if (existingDescriptor) {
      allDescriptors.push(existingDescriptor);
    } else {
      toLogicalRootCommentElement(descriptor);

      // Since we've already parsed the payloads from the start and end comments,
      // we sanitize them to reduce noise in the DOM.
      const { start, end } = descriptor;
      start.textContent = 'bl-root';
      if (end) {
        end.textContent = '/bl-root';
      }

      allDescriptors.push(descriptor);
    }
  }

  return allDescriptors;
}

function ensureEditableValueSynchronized(destination: Element, value: any) {
  if (destination instanceof HTMLTextAreaElement && destination.value !== value) {
    destination.value = value as string;
  } else if (destination instanceof HTMLSelectElement && destination.selectedIndex !== value) {
    destination.selectedIndex = value as number;
  } else if (destination instanceof HTMLInputElement) {
    if (destination.type === 'checkbox' || destination.type === 'radio') {
      if (destination.checked !== value) {
        destination.checked = value as boolean;
      }
    } else if (destination.value !== value) {
      destination.value = value as string;
    }
  }
}

function getEditableElementValue(elem: Element): string | boolean | number | null {
  if (elem instanceof HTMLSelectElement) {
    return elem.selectedIndex;
  } else if (elem instanceof HTMLInputElement) {
    return elem.type === 'checkbox' || elem.type === 'radio' ? elem.checked : (elem.getAttribute('value') || '');
  } else if (elem instanceof HTMLTextAreaElement) {
    return elem.value;
  } else {
    return null;
  }
}

export interface CommentBoundedRange {
  startExclusive: Comment,
  endExclusive: Comment,
}

interface EditWalker {
  current: Node;
  advance(): void;
}

class DomNodeEditWalker implements EditWalker {
  current: Node;

  constructor(startNode: Node) {
    this.current = startNode;
  }

  advance() {
    if (!this.current) {
      throw new Error('Cannot advance beyond the end of the sibling array');
    }

    this.current = this.current.nextSibling!;
  }
}

class LogicalElementEditWalker implements EditWalker {
  current: Node;

  constructor(startNode: LogicalElement) {
    this.current = startNode as unknown as Node;
  }

  advance() {
    if (!this.current) {
      throw new Error('Cannot advance beyond the end of the logical children array');
    }

    const nextSibling = getLogicalNextSibling(this.current as unknown as LogicalElement);
    this.current = nextSibling as unknown as Node;
  }
}

class SiblingSubsetNodeList implements ItemList<Node> {
  private readonly siblings: ItemList<Node>;

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

  constructor(childNodes: ItemList<Node>, range: CommentBoundedRange) {
    this.siblings = childNodes;
    this.startIndex = Array.prototype.indexOf.call(this.siblings, range.startExclusive) + 1;
    this.endIndexExcl = Array.prototype.indexOf.call(this.siblings, range.endExclusive);
    this.length = this.endIndexExcl - this.startIndex;
  }
}

class LogicalElementNodeList implements ItemList<Node> {
  readonly length: number;

  constructor(element: LogicalElement) {
    const childNodes = getLogicalChildrenArray(element);
    this.length = childNodes.length;

    // This is done for compatibility with Array.prototype.indexOf, which expects an array-like object
    Object.assign(this, childNodes);
  }

  [index: number]: Node;

  item(index: number): Node | null {
    return this[index] as unknown as Node || null;
  }

  forEach(callbackfn: (value: Node, key: number, parent: ItemList<Node>) => void, thisArg?: any): void {
    for (let i = 0; i < this.length; i++) {
      callbackfn.call(thisArg, this.item(i)!, i, this);
    }
  }
}
