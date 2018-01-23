import { System_Array, MethodHandle } from '../Platform/Platform';
import { getRenderTreeEditPtr, renderTreeEdit, EditType } from './RenderTreeEdit';
import { getTreeNodePtr, renderTreeNode, NodeType, RenderTreeNodePointer } from './RenderTreeNode';
import { platform } from '../Environment';
let raiseEventMethod: MethodHandle;
let renderComponentMethod: MethodHandle;

export class BrowserRenderer {
  // TODO: Instead of associating components to parent elements, associate them with a
  // start/end node, so that components don't have to be enclosed in a wrapper
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

    this.applyEdits(componentId, { parent: element, childIndex: 0 }, edits, editsLength, referenceTree);
  }

  applyEdits(componentId: number, location: DOMLocation, edits: System_Array, editsLength: number, referenceTree: System_Array) {
    for (let editIndex = 0; editIndex < editsLength; editIndex++) {
      const edit = getRenderTreeEditPtr(edits, editIndex);
      const editType = renderTreeEdit.type(edit);
      switch (editType) {
        case EditType.continue: {
          location.childIndex++;
          break;
        }
        case EditType.prependNode: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          this.insertNode(componentId, location, referenceTree, node, nodeIndex);
          break;
        }
        case EditType.removeNode: {
          removeNodeFromDOM(location);
          break;
        }
        case EditType.setAttribute: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          const element = location.parent.childNodes[location.childIndex] as HTMLElement;
          this.applyAttribute(componentId, element, node, nodeIndex);
          break;
        }
        case EditType.removeAttribute: {
          removeAttributeFromDOM(location, renderTreeEdit.removedAttributeName(edit)!);
          break;
        }
        case EditType.updateText: {
          const nodeIndex = renderTreeEdit.newTreeIndex(edit);
          const node = getTreeNodePtr(referenceTree, nodeIndex);
          const domTextNode = location.parent.childNodes[location.childIndex] as Text;
          domTextNode.textContent = renderTreeNode.textContent(node);
          break;
        }
        case EditType.stepIn: {
          location.parent = location.parent.childNodes[location.childIndex] as HTMLElement;
          location.childIndex = 0;
          break;
        }
        case EditType.stepOut: {
          // To avoid the indexOf, consider maintaining a stack of locations
          const targetElement = location.parent;
          location.parent = location.parent.parentElement!;
          location.childIndex = Array.prototype.indexOf.call(location.parent.childNodes, targetElement) + 1;
          break;
        }
        default: {
          const unknownType: never = editType; // Compile-time verification that the switch was exhaustive
          throw new Error(`Unknown edit type: ${unknownType}`);
        }
      }
    }
  }

  insertNode(componentId: number, location: DOMLocation, nodes: System_Array, node: RenderTreeNodePointer, nodeIndex: number) {
    const nodeType = renderTreeNode.nodeType(node);
    switch (nodeType) {
      case NodeType.element:
        this.insertElement(componentId, location, nodes, node, nodeIndex);
        break;
      case NodeType.text:
        this.insertText(location, node);
        break;
      case NodeType.attribute:
        throw new Error('Attribute nodes should only be present as leading children of element nodes.');
      case NodeType.component:
        this.insertComponent(location, node);
        break;
      default:
        const unknownType: never = nodeType; // Compile-time verification that the switch was exhaustive
        throw new Error(`Unknown node type: ${unknownType}`);
    }
  }

  insertElement(componentId: number, location: DOMLocation, nodes: System_Array, node: RenderTreeNodePointer, nodeIndex: number) {
    const tagName = renderTreeNode.elementName(node)!;
    const newDomElement = document.createElement(tagName);
    insertNodeIntoDOM(newDomElement, location);

    // Apply attributes
    const descendantsEndIndex = renderTreeNode.descendantsEndIndex(node);
    for (let descendantIndex = nodeIndex + 1; descendantIndex <= descendantsEndIndex; descendantIndex++) {
      const descendantNode = getTreeNodePtr(nodes, descendantIndex);
      if (renderTreeNode.nodeType(descendantNode) === NodeType.attribute) {
        this.applyAttribute(componentId, newDomElement, descendantNode, descendantIndex);
      } else {
        // As soon as we see a non-attribute child, all the subsequent child nodes are
        // not attributes, so bail out and insert the remnants recursively
        this.insertNodeRange(componentId, { parent: newDomElement, childIndex: 0 }, nodes, descendantIndex, descendantsEndIndex);
        break;
      }
    }
  }

  insertComponent(location: DOMLocation, node: RenderTreeNodePointer) {
    const containerElement = document.createElement('blazor-component');
    insertNodeIntoDOM(containerElement, location);

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

  insertText(location: DOMLocation, textNode: RenderTreeNodePointer) {
    const textContent = renderTreeNode.textContent(textNode)!;
    const newDomTextNode = document.createTextNode(textContent);
    insertNodeIntoDOM(newDomTextNode, location);
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

  insertNodeRange(componentId: number, location: DOMLocation, nodes: System_Array, startIndex: number, endIndex: number) {
    for (let index = startIndex; index <= endIndex; index++) {
      const node = getTreeNodePtr(nodes, index);
      this.insertNode(componentId, location, nodes, node, index);

      // Skip over any descendants, since they are already dealt with recursively
      const descendantsEndIndex = renderTreeNode.descendantsEndIndex(node);
      if (descendantsEndIndex > 0) {
        index = descendantsEndIndex;
      }
    }
  }
}

export interface DOMLocation {
  parent: Element;
  childIndex: number;
}

function insertNodeIntoDOM(node: Node, location: DOMLocation) {
  const parent = location.parent;
  if (location.childIndex >= parent.childNodes.length) {
    parent.appendChild(node);
  } else {
    parent.insertBefore(node, parent.childNodes[location.childIndex]);
  }
  location.childIndex++;
}

function removeNodeFromDOM(location: DOMLocation) {
  const parent = location.parent;
  parent.removeChild(parent.childNodes[location.childIndex]);
}

function removeAttributeFromDOM(location: DOMLocation, attributeName: string) {
  const element = location.parent.childNodes[location.childIndex] as HTMLElement;
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
