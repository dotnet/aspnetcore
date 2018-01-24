import { System_Array, MethodHandle } from '../Platform/Platform';
import { getRenderTreeEditPtr, renderTreeEdit, EditType } from './RenderTreeEdit';
import { getTreeNodePtr, renderTreeNode, NodeType, RenderTreeNodePointer } from './RenderTreeNode';
import { platform } from '../Environment';
let raiseEventMethod: MethodHandle;
let renderComponentMethod: MethodHandle;

export class BrowserRenderer {
  // TODO: To avoid leaking memory, automatically remove entries from this dict as soon
  // as the corresponding DOM nodes are removed (or maybe when the associated component
  // is disposed, assuming we can guarantee that always happens).
  private childComponentLocations: { [componentId: number]: Element } = {};

  constructor(private browserRendererId: number) {
  }

  public attachComponentToElement(componentId: number, element: Element) {
    this.childComponentLocations[componentId] = element;
  }

  public updateComponent(componentId: number, edits: System_Array, editsLength: number, referenceTree: System_Array) {
    const element = this.childComponentLocations[componentId];
    if (!element) {
      throw new Error(`No element is currently associated with component ${componentId}`);
    }

    this.applyEdits(componentId, element, 0, edits, editsLength, referenceTree);
  }

  applyEdits(componentId: number, parent: Element, childIndex: number, edits: System_Array, editsLength: number, referenceTree: System_Array) {
    const childIndexStack: number[] = []; // TODO: This can be removed. We only (potentially) have nonzero childIndex values at the root, so we only need to track the current depth to determine whether we are at the root
    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const edit = getRenderTreeEditPtr(edits, editIndex);
      const editType = renderTreeEdit.type(edit);
      switch (editType) {
        case EditType.prependNode: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          this.insertNode(componentId, parent, childIndex + siblingIndex, referenceTree, node, nodeIndex);
          break;
        }
        case EditType.removeNode: {
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          removeNodeFromDOM(parent, childIndex + siblingIndex);
          break;
        }
        case EditType.setAttribute: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          const element = parent.childNodes[childIndex + siblingIndex] as HTMLElement;
          this.applyAttribute(componentId, element, node, nodeIndex);
          break;
        }
        case EditType.removeAttribute: {
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          removeAttributeFromDOM(parent, childIndex + siblingIndex, renderTreeEdit.removedAttributeName(edit)!);
          break;
        }
        case EditType.updateText: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          const domTextNode = parent.childNodes[childIndex + siblingIndex] as Text;
          domTextNode.textContent = renderTreeNode.textContent(node);
          break;
        }
        case EditType.stepIn: {
          childIndexStack.push(childIndex);
          const siblingIndex = renderTreeEdit.siblingIndex(edit);
          parent = parent.childNodes[childIndex + siblingIndex] as HTMLElement;
          childIndex = 0;
          break;
        }
        case EditType.stepOut: {
          parent = parent.parentElement!;
          childIndex = childIndexStack.pop()!;
          break;
        }
        default: {
          const unknownType: never = editType; // Compile-time verification that the switch was exhaustive
          throw new Error(`Unknown edit type: ${unknownType}`);
        }
      }
    }
  }

  insertNode(componentId: number, parent: Element, childIndex: number, nodes: System_Array, node: RenderTreeNodePointer, nodeIndex: number) {
    const nodeType = renderTreeNode.nodeType(node);
    switch (nodeType) {
      case NodeType.element:
        this.insertElement(componentId, parent, childIndex, nodes, node, nodeIndex);
        break;
      case NodeType.text:
        this.insertText(parent, childIndex, node);
        break;
      case NodeType.attribute:
        throw new Error('Attribute nodes should only be present as leading children of element nodes.');
      case NodeType.component:
        this.insertComponent(parent, childIndex, node);
        break;
      default:
        const unknownType: never = nodeType; // Compile-time verification that the switch was exhaustive
        throw new Error(`Unknown node type: ${unknownType}`);
    }
  }

  insertElement(componentId: number, parent: Element, childIndex: number, nodes: System_Array, node: RenderTreeNodePointer, nodeIndex: number) {
    const tagName = renderTreeNode.elementName(node)!;
    const newDomElement = document.createElement(tagName);
    insertNodeIntoDOM(newDomElement, parent, childIndex);

    // Apply attributes
    const descendantsEndIndex = renderTreeNode.descendantsEndIndex(node);
    for (let descendantIndex = nodeIndex + 1; descendantIndex <= descendantsEndIndex; descendantIndex++) {
      const descendantNode = getTreeNodePtr(nodes, descendantIndex);
      if (renderTreeNode.nodeType(descendantNode) === NodeType.attribute) {
        this.applyAttribute(componentId, newDomElement, descendantNode, descendantIndex);
      } else {
        // As soon as we see a non-attribute child, all the subsequent child nodes are
        // not attributes, so bail out and insert the remnants recursively
        this.insertNodeRange(componentId, newDomElement, 0, nodes, descendantIndex, descendantsEndIndex);
        break;
      }
    }
  }

  insertComponent(parent: Element, childIndex: number, node: RenderTreeNodePointer) {
    // Currently, to support O(1) lookups from render tree nodes to DOM nodes, we rely on
    // each child component existing as a single top-level element in the DOM. To guarantee
    // that, we wrap child components in these 'blazor-component' wrappers.
    // To improve on this in the future:
    // - If we can statically detect that a given component always produces a single top-level
    //   element anyway, then don't wrap it in a further nonstandard element
    // - If we really want to support child components producing multiple top-level nodes and
    //   not being wrapped in a container at all, then every time a component is refreshed in
    //   the DOM, we could update an array on the parent element that specifies how many DOM
    //   nodes correspond to each of its render tree nodes. Then when that parent wants to
    //   locate the first DOM node for a render tree node, it can sum all the node counts for
    //   all the preceding render trees nodes. It's O(N), but where N is the number of siblings
    //   (counting child components as a single item), so N will rarely if ever be large.
    //   We could even keep track of whether all the child components happen to have exactly 1
    //   top level node, and in that case, there's no need to sum as we can do direct lookups.
    const containerElement = document.createElement('blazor-component');
    insertNodeIntoDOM(containerElement, parent, childIndex);

    const childComponentId = renderTreeNode.componentId(node);
    this.attachComponentToElement(childComponentId, containerElement);

    if (!renderComponentMethod) {
      renderComponentMethod = platform.findMethod(
        'Microsoft.Blazor.Browser', 'Microsoft.Blazor.Browser.Rendering', 'BrowserRendererEventDispatcher', 'RenderChildComponent'
      );
    }

    // TODO: Consider caching the .NET string instance for this.browserRendererId
    platform.callMethod(renderComponentMethod, null, [
      platform.toDotNetString(this.browserRendererId.toString()),
      platform.toDotNetString(childComponentId.toString())
    ]);
  }

  insertText(parent: Element, childIndex: number, textNode: RenderTreeNodePointer) {
    const textContent = renderTreeNode.textContent(textNode)!;
    const newDomTextNode = document.createTextNode(textContent);
    insertNodeIntoDOM(newDomTextNode, parent, childIndex);
  }

  applyAttribute(componentId: number, toDomElement: Element, attributeNode: RenderTreeNodePointer, attributeNodeIndex: number) {
    const attributeName = renderTreeNode.attributeName(attributeNode)!;
    const browserRendererId = this.browserRendererId;

    // TODO: Instead of applying separate event listeners to each DOM element, use event delegation
    // and remove all the _blazor*Listener hacks
    switch (attributeName) {
      case 'onclick': {
        toDomElement.removeEventListener('click', toDomElement['_blazorClickListener']);
        const listener = () => raiseEvent(browserRendererId, componentId, attributeNodeIndex, 'mouse', { Type: 'click' });
        toDomElement['_blazorClickListener'] = listener;
        toDomElement.addEventListener('click', listener);
        break;
      }
      case 'onkeypress': {
        toDomElement.removeEventListener('keypress', toDomElement['_blazorKeypressListener']);
        const listener = evt => {
          // This does not account for special keys nor cross-browser differences. So far it's
          // just to establish that we can pass parameters when raising events.
          // We use C#-style PascalCase on the eventInfo to simplify deserialization, but this could
          // change if we introduced a richer JSON library on the .NET side.
          raiseEvent(browserRendererId, componentId, attributeNodeIndex, 'keyboard', { Type: evt.type, Key: (evt as any).key });
        };
        toDomElement['_blazorKeypressListener'] = listener;
        toDomElement.addEventListener('keypress', listener);
        break;
      }
      default:
        // Treat as a regular string-valued attribute
        toDomElement.setAttribute(
          attributeName,
          renderTreeNode.attributeValue(attributeNode)!
        );
        break;
    }
  }

  insertNodeRange(componentId: number, parent: Element, childIndex: number, nodes: System_Array, startIndex: number, endIndex: number) {
    for (let index = startIndex; index <= endIndex; index++) {
      const node = getTreeNodePtr(nodes, index);
      this.insertNode(componentId, parent, childIndex, nodes, node, index);
      childIndex++;

      // Skip over any descendants, since they are already dealt with recursively
      const descendantsEndIndex = renderTreeNode.descendantsEndIndex(node);
      if (descendantsEndIndex > 0) {
        index = descendantsEndIndex;
      }
    }
  }
}

function insertNodeIntoDOM(node: Node, parent: Element, childIndex: number) {
  if (childIndex >= parent.childNodes.length) {
    parent.appendChild(node);
  } else {
    parent.insertBefore(node, parent.childNodes[childIndex]);
  }
}

function removeNodeFromDOM(parent: Element, childIndex: number) {
  parent.removeChild(parent.childNodes[childIndex]);
}

function removeAttributeFromDOM(parent: Element, childIndex: number, attributeName: string) {
  const element = parent.childNodes[childIndex] as Element;
  element.removeAttribute(attributeName);
}

function raiseEvent(browserRendererId: number, componentId: number, renderTreeNodeIndex: number, eventInfoType: EventInfoType, eventInfo: any) {
  if (!raiseEventMethod) {
    raiseEventMethod = platform.findMethod(
      'Microsoft.Blazor.Browser', 'Microsoft.Blazor.Browser.Rendering', 'BrowserRendererEventDispatcher', 'DispatchEvent'
    );
  }

  const eventDescriptor = {
    BrowserRendererId: browserRendererId,
    ComponentId: componentId,
    RenderTreeNodeIndex: renderTreeNodeIndex,
    EventArgsType: eventInfoType
  };

  platform.callMethod(raiseEventMethod, null, [
    platform.toDotNetString(JSON.stringify(eventDescriptor)),
    platform.toDotNetString(JSON.stringify(eventInfo))
  ]);
}

type EventInfoType = 'mouse' | 'keyboard';
