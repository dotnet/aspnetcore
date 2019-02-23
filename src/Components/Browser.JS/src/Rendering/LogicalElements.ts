/*
  A LogicalElement plays the same role as an Element instance from the point of view of the
  API consumer. Inserting and removing logical elements updates the browser DOM just the same.

  The difference is that, unlike regular DOM mutation APIs, the LogicalElement APIs don't use
  the underlying DOM structure as the data storage for the element hierarchy. Instead, the
  LogicalElement APIs take care of tracking hierarchical relationships separately. The point
  of this is to permit a logical tree structure in which parent/child relationships don't
  have to be materialized in terms of DOM element parent/child relationships. And the reason
  why we want that is so that hierarchies of Blazor components can be tracked even when those
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

const logicalChildrenPropname = createSymbolOrFallback('_blazorLogicalChildren');
const logicalParentPropname = createSymbolOrFallback('_blazorLogicalParent');

export function toLogicalElement(element: Element, allowExistingContents?: boolean) {
  // Normally it's good to assert that the element has started empty, because that's the usual
  // situation and we probably have a bug if it's not. But for the element that contain prerendered
  // root components, we want to let them keep their content until we replace it.
  if (element.childNodes.length > 0 && !allowExistingContents) {
    throw new Error('New logical elements must start empty, or allowExistingContents must be true');
  }

  element[logicalChildrenPropname] = [];
  return element as any as LogicalElement;
}

export function createAndInsertLogicalContainer(parent: LogicalElement, childIndex: number): LogicalElement {
  const containerElement = document.createComment('!');
  insertLogicalChild(containerElement, parent, childIndex);
  return containerElement as any as LogicalElement;
}

export function insertLogicalChild(child: Node, parent: LogicalElement, childIndex: number) {
  const childAsLogicalElement = child as any as LogicalElement;
  if (child instanceof Comment) {
    const existingGrandchildren = getLogicalChildrenArray(childAsLogicalElement);
    if (existingGrandchildren && getLogicalChildrenArray(childAsLogicalElement).length > 0) {
      // There's nothing to stop us implementing support for this scenario, and it's not difficult
      // (after inserting 'child' itself, also iterate through its logical children and physically
      // put them as following-siblings in the DOM). However there's no scenario that requires it
      // presently, so if we did implement it there'd be no good way to have tests for it.
      throw new Error('Not implemented: inserting non-empty logical container');
    }
  }

  if (getLogicalParent(childAsLogicalElement)) {
    // Likewise, we could easily support this scenario too (in this 'if' block, just splice
    // out 'child' from the logical children array of its previous logical parent by using
    // Array.prototype.indexOf to determine its previous sibling index).
    // But again, since there's not currently any scenario that would use it, we would not
    // have any test coverage for such an implementation.
    throw new Error('Not implemented: moving existing logical children');
  }

  const newSiblings = getLogicalChildrenArray(parent);
  if (childIndex < newSiblings.length) {
    // Insert
    const nextSibling = newSiblings[childIndex] as any as Node;
    nextSibling.parentNode!.insertBefore(child, nextSibling);
    newSiblings.splice(childIndex, 0, childAsLogicalElement);
  } else {
    // Append
    appendDomNode(child, parent);
    newSiblings.push(childAsLogicalElement);
  }

  childAsLogicalElement[logicalParentPropname] = parent;
  if (!(logicalChildrenPropname in childAsLogicalElement)) {
    childAsLogicalElement[logicalChildrenPropname] = [];
  }
}

export function removeLogicalChild(parent: LogicalElement, childIndex: number) {
  const childrenArray = getLogicalChildrenArray(parent);
  const childToRemove = childrenArray.splice(childIndex, 1)[0];

  // If it's a logical container, also remove its descendants
  if (childToRemove instanceof Comment) {
    const grandchildrenArray = getLogicalChildrenArray(childToRemove);
    while (grandchildrenArray.length > 0) {
      removeLogicalChild(childToRemove, 0);
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

export function isSvgElement(element: LogicalElement) {
  return getClosestDomElement(element).namespaceURI === 'http://www.w3.org/2000/svg';
}

function getLogicalChildrenArray(element: LogicalElement) {
  return element[logicalChildrenPropname] as LogicalElement[];
}

function getLogicalNextSibling(element: LogicalElement): LogicalElement | null {
  const siblings = getLogicalChildrenArray(getLogicalParent(element)!);
  const siblingIndex = Array.prototype.indexOf.call(siblings, element);
  return siblings[siblingIndex + 1] || null;
}

function getClosestDomElement(logicalElement: LogicalElement) {
  if (logicalElement instanceof Element) {
    return logicalElement;
  } else if (logicalElement instanceof Comment) {
    return logicalElement.parentNode! as Element;
  } else {
    throw new Error('Not a valid logical element');
  }
}

function appendDomNode(child: Node, parent: LogicalElement) {
  // This function only puts 'child' into the DOM in the right place relative to 'parent'
  // It does not update the logical children array of anything
  if (parent instanceof Element) {
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

function createSymbolOrFallback(fallback: string): symbol | string {
  return typeof Symbol === 'function' ? Symbol() : fallback;
}

// Nominal type to represent a logical element without needing to allocate any object for instances
export interface LogicalElement { LogicalElement__DO_NOT_IMPLEMENT: any };
