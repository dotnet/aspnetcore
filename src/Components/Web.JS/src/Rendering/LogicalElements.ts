// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from '../Services/ComponentDescriptorDiscovery';

/*
  A LogicalElement plays the same role as an Element instance from the point of view of the
  API consumer. Inserting and removing logical elements updates the browser DOM just the same.

  The difference is that, unlike regular DOM mutation APIs, the LogicalElement APIs don't use
  the underlying DOM structure as the data storage for the element hierarchy. Instead, the
  LogicalElement APIs take care of tracking hierarchical relationships separately. The point
  of this is to permit a logical tree structure in which parent/child relationships don't
  have to be materialized in terms of DOM element parent/child relationships. And the reason
  why we want that is so that hierarchies of Razor components can be tracked even when those
  components' render output need not be a single literal DOM element.

  Consumers of the API don't need to know about the implementation, but how it's done is:
  - Each LogicalElement is materialized in the DOM as either:
    - A Node instance, for actual Node instances inserted using 'insertLogicalChild' or
      for Element instances promoted to LogicalElement via 'toLogicalElement'
    - A Comment instance, for 'logical container' instances inserted using 'createAndInsertLogicalContainer'
  - Then, on that instance (i.e., the Node or Comment), we store an array of 'logical children'
    instances, e.g.,
      [firstChild, secondChild, thirdChild, ...]
    ... plus we store a reference to the 'logical parent' (if any)
  - The 'logical children' array means we can look up in O(1):
    - The number of logical children (not currently implemented because not required, but trivial)
    - The logical child at any given index
  - Whenever a logical child is added or removed, we update the parent's array of logical children
*/

const logicalChildrenPropname = Symbol();
const logicalParentPropname = Symbol();
const logicalRootDescriptorPropname = Symbol();

export function toLogicalRootCommentElement(descriptor: ComponentDescriptor): LogicalElement {
  // Now that we support start/end comments as component delimiters we are going to be setting up
  // adding the components rendered output as siblings of the start/end tags (between).
  // For that to work, we need to appropriately configure the parent element to be a logical element
  // with all their children being the child elements.
  // For example, imagine you have
  // <app>
  // <div><p>Static content</p></div>
  // <!-- start component
  // <!-- end component
  // <footer>Some other content</footer>
  // <app>
  // We want the parent element to be something like
  // *app
  // |- *div
  // |- *component
  // |- *footer
  const { start, end } = descriptor;
  const existingDescriptor = start[logicalRootDescriptorPropname];
  if (existingDescriptor) {
    if (existingDescriptor !== descriptor) {
      throw new Error('The start component comment was already associated with another component descriptor.');
    }
    return start as unknown as LogicalElement;
  }

  const parent = start.parentNode;
  if (!parent) {
    throw new Error(`Comment not connected to the DOM ${start.textContent}`);
  }

  const parentLogicalElement = toLogicalElement(parent, /* allow existing contents */ true);
  const children = getLogicalChildrenArray(parentLogicalElement);

  start[logicalParentPropname] = parentLogicalElement;
  start[logicalRootDescriptorPropname] = descriptor;
  const startLogicalElement = toLogicalElement(start);

  if (end) {
    // We need to make each element between the start and end comments a logical child
    // of the start node.
    const rootCommentChildren = getLogicalChildrenArray(startLogicalElement);
    const startNextChildIndex = Array.prototype.indexOf.call(children, startLogicalElement) + 1;
    let lastMovedChild: LogicalElement | null = null;

    while (lastMovedChild !== end as unknown as LogicalElement) {
      const childToMove = children.splice(startNextChildIndex, 1)[0];
      if (!childToMove) {
        throw new Error('Could not find the end component comment in the parent logical node list');
      }
      childToMove[logicalParentPropname] = start;
      rootCommentChildren.push(childToMove);
      lastMovedChild = childToMove;
    }
  }

  return startLogicalElement;
}

export function toLogicalElement(element: Node, allowExistingContents?: boolean): LogicalElement {
  if (logicalChildrenPropname in element) { // If it's already a logical element, leave it alone
    return element as unknown as LogicalElement;
  }

  const childrenArray: LogicalElement[] = [];

  if (element.childNodes.length > 0) {
    // Normally it's good to assert that the element has started empty, because that's the usual
    // situation and we probably have a bug if it's not. But for the elements that contain prerendered
    // root components, we want to let them keep their content until we replace it.
    if (!allowExistingContents) {
      throw new Error('New logical elements must start empty, or allowExistingContents must be true');
    }

    element.childNodes.forEach(child => {
      const childLogicalElement = toLogicalElement(child, /* allowExistingContents */ true);
      childLogicalElement[logicalParentPropname] = element;
      childrenArray.push(childLogicalElement);
    });
  }

  element[logicalChildrenPropname] = childrenArray;
  return element as unknown as LogicalElement;
}

export function emptyLogicalElement(element: LogicalElement): void {
  const childrenArray = getLogicalChildrenArray(element);
  while (childrenArray.length) {
    removeLogicalChild(element, 0);
  }
}

export function createAndInsertLogicalContainer(parent: LogicalElement, childIndex: number): LogicalElement {
  const containerElement = document.createComment('!');
  insertLogicalChild(containerElement, parent, childIndex);
  return containerElement as unknown as LogicalElement;
}

export function insertLogicalChildBefore(child: Node, parent: LogicalElement, before: LogicalElement | null): void {
  const childrenArray = getLogicalChildrenArray(parent);
  let childIndex: number;
  if (before) {
    childIndex = Array.prototype.indexOf.call(childrenArray, before);
    if (childIndex < 0) {
      throw new Error('Could not find logical element in the parent logical node list');
    }
  } else {
    childIndex = childrenArray.length;
  }
  insertLogicalChild(child, parent, childIndex);
}

export function insertLogicalChild(child: Node, parent: LogicalElement, childIndex: number): void {
  const childAsLogicalElement = child as unknown as LogicalElement;

  // If the child is a component comment with logical children, its children
  // need to be inserted into the parent node
  let nodeToInsert = child;
  if (child instanceof Comment) {
    const existingGranchildren = getLogicalChildrenArray(childAsLogicalElement);
    if (existingGranchildren?.length > 0) {
      const lastNodeToInsert = findLastDomNodeInRange(childAsLogicalElement);
      const range = new Range();
      range.setStartBefore(child);
      range.setEndAfter(lastNodeToInsert);
      nodeToInsert = range.extractContents();
    }
  }

  // If the node we're inserting already has a logical parent,
  // remove it from its sibling array
  const existingLogicalParent = getLogicalParent(childAsLogicalElement);
  if (existingLogicalParent) {
    const existingSiblingArray = getLogicalChildrenArray(existingLogicalParent);
    const existingChildIndex = Array.prototype.indexOf.call(existingSiblingArray, childAsLogicalElement);
    existingSiblingArray.splice(existingChildIndex, 1);
    delete childAsLogicalElement[logicalParentPropname];
  }

  const newSiblings = getLogicalChildrenArray(parent);
  if (childIndex < newSiblings.length) {
    // Insert
    const nextSibling = newSiblings[childIndex] as any as Node;
    nextSibling.parentNode!.insertBefore(nodeToInsert, nextSibling);
    newSiblings.splice(childIndex, 0, childAsLogicalElement);
  } else {
    // Append
    appendDomNode(nodeToInsert, parent);
    newSiblings.push(childAsLogicalElement);
  }

  childAsLogicalElement[logicalParentPropname] = parent;
  if (!(logicalChildrenPropname in childAsLogicalElement)) {
    childAsLogicalElement[logicalChildrenPropname] = [];
  }
}

export function removeLogicalChild(parent: LogicalElement, childIndex: number): void {
  const childrenArray = getLogicalChildrenArray(parent);
  const childToRemove = childrenArray.splice(childIndex, 1)[0];

  // If it's a logical container, also remove its descendants
  if (childToRemove instanceof Comment) {
    const grandchildrenArray = getLogicalChildrenArray(childToRemove);
    if (grandchildrenArray) {
      while (grandchildrenArray.length > 0) {
        removeLogicalChild(childToRemove, 0);
      }
    }
  }

  // Finally, remove the node itself
  const domNodeToRemove = childToRemove as any as Node;
  domNodeToRemove.parentNode!.removeChild(domNodeToRemove);
}

export function getLogicalParent(element: LogicalElement): LogicalElement | null {
  return (element[logicalParentPropname] as LogicalElement) || null;
}

export function getLogicalChild(parent: LogicalElement, childIndex: number): LogicalElement {
  return getLogicalChildrenArray(parent)[childIndex];
}

export function getLogicalRootDescriptor(element: LogicalElement): ComponentDescriptor {
  return element[logicalRootDescriptorPropname] || null;
}

// SVG elements support `foreignObject` children that can hold arbitrary HTML.
// For these scenarios, the parent SVG and `foreignObject` elements should
// be rendered under the SVG namespace, while the HTML content should be rendered
// under the XHTML namespace. If the correct namespaces are not provided, most
// browsers will fail to render the foreign object content. Here, we ensure that if
// we encounter a `foreignObject` in the SVG, then all its children will be placed
// under the XHTML namespace.
export function isSvgElement(element: LogicalElement): boolean {
  // Note: This check is intentionally case-sensitive since we expect this element
  // to appear as a child of an SVG element and SVGs are case-sensitive.
  const closestElement = getClosestDomElement(element) as any;
  return closestElement.namespaceURI === 'http://www.w3.org/2000/svg' && closestElement['tagName'] !== 'foreignObject';
}

export function getLogicalChildrenArray(element: LogicalElement): LogicalElement[] {
  return element[logicalChildrenPropname] as LogicalElement[];
}

export function getLogicalNextSibling(element: LogicalElement): LogicalElement | null {
  const siblings = getLogicalChildrenArray(getLogicalParent(element)!);
  const siblingIndex = Array.prototype.indexOf.call(siblings, element);
  return siblings[siblingIndex + 1] || null;
}

export function isLogicalElement(element: Node): boolean {
  return logicalChildrenPropname in element;
}

export function permuteLogicalChildren(parent: LogicalElement, permutationList: PermutationListEntry[]): void {
  // The permutationList must represent a valid permutation, i.e., the list of 'from' indices
  // is distinct, and the list of 'to' indices is a permutation of it. The algorithm here
  // relies on that assumption.

  // Each of the phases here has to happen separately, because each one is designed not to
  // interfere with the indices or DOM entries used by subsequent phases.

  // Phase 1: track which nodes we will move
  const siblings = getLogicalChildrenArray(parent);
  permutationList.forEach((listEntry: PermutationListEntryWithTrackingData) => {
    listEntry.moveRangeStart = siblings[listEntry.fromSiblingIndex];
    listEntry.moveRangeEnd = findLastDomNodeInRange(listEntry.moveRangeStart);
  });

  // Phase 2: insert markers
  permutationList.forEach((listEntry: PermutationListEntryWithTrackingData) => {
    const marker = document.createComment('marker');
    listEntry.moveToBeforeMarker = marker;
    const insertBeforeNode = siblings[listEntry.toSiblingIndex + 1] as any as Node;
    if (insertBeforeNode) {
      insertBeforeNode.parentNode!.insertBefore(marker, insertBeforeNode);
    } else {
      appendDomNode(marker, parent);
    }
  });

  // Phase 3: move descendants & remove markers
  permutationList.forEach((listEntry: PermutationListEntryWithTrackingData) => {
    const insertBefore = listEntry.moveToBeforeMarker!;
    const parentDomNode = insertBefore.parentNode!;
    const elementToMove = listEntry.moveRangeStart!;
    const moveEndNode = listEntry.moveRangeEnd!;
    let nextToMove = elementToMove as unknown as Node | null;
    while (nextToMove) {
      const nextNext = nextToMove.nextSibling;
      parentDomNode.insertBefore(nextToMove, insertBefore);

      if (nextToMove === moveEndNode) {
        break;
      } else {
        nextToMove = nextNext;
      }
    }

    parentDomNode.removeChild(insertBefore);
  });

  // Phase 4: update siblings index
  permutationList.forEach((listEntry: PermutationListEntryWithTrackingData) => {
    siblings[listEntry.toSiblingIndex] = listEntry.moveRangeStart!;
  });
}

export function getClosestDomElement(logicalElement: LogicalElement): Element | (LogicalElement & DocumentFragment) {
  if (logicalElement instanceof Element || logicalElement instanceof DocumentFragment) {
    return logicalElement;
  } else if (logicalElement instanceof Comment) {
    return logicalElement.parentNode! as Element;
  } else {
    throw new Error('Not a valid logical element');
  }
}

export interface PermutationListEntry {
  fromSiblingIndex: number,
  toSiblingIndex: number,
}

interface PermutationListEntryWithTrackingData extends PermutationListEntry {
  // These extra properties are used internally when processing the permutation list
  moveRangeStart?: LogicalElement,
  moveRangeEnd?: Node,
  moveToBeforeMarker?: Node,
}

function appendDomNode(child: Node, parent: LogicalElement) {
  // This function only puts 'child' into the DOM in the right place relative to 'parent'
  // It does not update the logical children array of anything
  if (parent instanceof Element || parent instanceof DocumentFragment) {
    parent.appendChild(child);
  } else if (parent instanceof Comment) {
    const parentLogicalNextSibling = getLogicalNextSibling(parent) as any as Node;
    if (parentLogicalNextSibling) {
      // Since the parent has a logical next-sibling, its appended child goes right before that
      parentLogicalNextSibling.parentNode!.insertBefore(child, parentLogicalNextSibling);
    } else {
      // Since the parent has no logical next-sibling, keep recursing upwards until we find
      // a logical ancestor that does have a next-sibling or is a physical element.
      appendDomNode(child, getLogicalParent(parent)!);
    }
  } else {
    // Should never happen
    throw new Error(`Cannot append node because the parent is not a valid logical element. Parent: ${parent}`);
  }
}

// Returns the final node (in depth-first evaluation order) that is a descendant of the logical element.
// As such, the entire subtree is between 'element' and 'findLastDomNodeInRange(element)' inclusive.
function findLastDomNodeInRange(element: LogicalElement): Node {
  if (element instanceof Element || element instanceof DocumentFragment) {
    return element;
  }

  const nextSibling = getLogicalNextSibling(element);
  if (nextSibling) {
    // Simple case: not the last logical sibling, so take the node before the next sibling
    return (nextSibling as any as Node).previousSibling!;
  } else {
    // Harder case: there's no logical next-sibling, so recurse upwards until we find
    // a logical ancestor that does have one, or a physical element
    const logicalParent = getLogicalParent(element)!;
    return logicalParent instanceof Element || logicalParent instanceof DocumentFragment
      ? logicalParent.lastChild!
      : findLastDomNodeInRange(logicalParent);
  }
}

// Nominal type to represent a logical element without needing to allocate any object for instances
export interface LogicalElement { LogicalElement__DO_NOT_IMPLEMENT: any }
