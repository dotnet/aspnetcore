import { registerFunction } from '../RegisteredFunction';
import { System_Object, System_String, System_Array, MethodHandle, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
import { getTreeNodePtr, renderTreeNode, NodeType, RenderTreeNodePointer } from './RenderTreeNode';
let raiseEventMethod: MethodHandle;
let renderComponentMethod: MethodHandle;

// TODO: Instead of associating components to parent elements, associate them with a
// start/end node, so that components don't have to be enclosed in a wrapper
// TODO: To avoid leaking memory, automatically remove entries from this dict as soon
// as the corresponding DOM nodes are removed (or maybe when the associated component
// is disposed, assuming we can guarantee that always happens).
type ComponentIdToParentElement = { [componentId: number]: Element };
type BrowserRendererRegistry = { [browserRendererId: number]: ComponentIdToParentElement };
const browserRenderers: BrowserRendererRegistry = {};

registerFunction('_blazorAttachComponentToElement', attachComponentToElement);
registerFunction('_blazorRender', renderRenderTree);

function attachComponentToElement(browserRendererId: number, elementSelector: System_String, componentId: number) {
  const elementSelectorJs = platform.toJavaScriptString(elementSelector);
  const element = document.querySelector(elementSelectorJs);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelectorJs}'.`);
  }

  browserRenderers[browserRendererId] = browserRenderers[browserRendererId] || {};
  browserRenderers[browserRendererId][componentId] = element;
}

function renderRenderTree(renderComponentArgs: Pointer) {
  const browserRendererId = platform.readHeapInt32(renderComponentArgs, 0);
  const browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    throw new Error(`There is no browser renderer with ID ${browserRendererId}.`);
  }

  const componentId = platform.readHeapInt32(renderComponentArgs, 4);
  const element = browserRenderer[componentId];
  if (!element) {
    throw new Error(`No element is currently associated with component ${componentId}`);
  }

  clearElement(element);

  const tree = platform.readHeapObject(renderComponentArgs, 8) as System_Array;
  const treeLength = platform.readHeapInt32(renderComponentArgs, 12);
  insertNodeRange(browserRendererId, componentId, element, tree, 0, treeLength - 1);
}

function insertNodeRange(browserRendererId: number, componentId: number, intoDomElement: Element, tree: System_Array, startIndex: number, endIndex: number) {
  for (let index = startIndex; index <= endIndex; index++) {
    const node = getTreeNodePtr(tree, index);
    insertNode(browserRendererId, componentId, intoDomElement, tree, node, index);

    // Skip over any descendants, since they are already dealt with recursively
    const descendantsEndIndex = renderTreeNode.descendantsEndIndex(node);
    if (descendantsEndIndex > 0) {
      index = descendantsEndIndex;
    }
  }
}

function insertNode(browserRendererId: number, componentId: number, intoDomElement: Element, tree: System_Array, node: RenderTreeNodePointer, nodeIndex: number) {
  const nodeType = renderTreeNode.nodeType(node);
  switch (nodeType) {
    case NodeType.element:
      insertElement(browserRendererId, componentId, intoDomElement, tree, node, nodeIndex);
      break;
    case NodeType.text:
      insertText(intoDomElement, node);
      break;
    case NodeType.attribute:
      throw new Error('Attribute nodes should only be present as leading children of element nodes.');
    case NodeType.component:
      insertComponent(browserRendererId, intoDomElement, node);
      break;
    default:
      const unknownType: never = nodeType; // Compile-time verification that the switch was exhaustive
      throw new Error(`Unknown node type: ${ unknownType }`);
  }
}

function insertComponent(browserRendererId: number, intoDomElement: Element, node: RenderTreeNodePointer) {
  const containerElement = document.createElement('blazor-component');
  intoDomElement.appendChild(containerElement);

  var childComponentId = renderTreeNode.componentId(node);
  browserRenderers[browserRendererId][childComponentId] = containerElement;

  if (!renderComponentMethod) {
    renderComponentMethod = platform.findMethod(
      'Microsoft.Blazor.Browser', 'Microsoft.Blazor.Browser.Rendering', 'BrowserRendererEventDispatcher', 'RenderChildComponent'
    );
  }

  platform.callMethod(renderComponentMethod, null, [
    platform.toDotNetString(browserRendererId.toString()),
    platform.toDotNetString(childComponentId.toString())
  ]);
}

function insertElement(browserRendererId: number, componentId: number, intoDomElement: Element, tree: System_Array, elementNode: RenderTreeNodePointer, elementNodeIndex: number) {
  const tagName = renderTreeNode.elementName(elementNode);
  const newDomElement = document.createElement(tagName);
  intoDomElement.appendChild(newDomElement);

  // Apply attributes
  const descendantsEndIndex = renderTreeNode.descendantsEndIndex(elementNode);
  for (let descendantIndex = elementNodeIndex + 1; descendantIndex <= descendantsEndIndex; descendantIndex++) {
    const descendantNode = getTreeNodePtr(tree, descendantIndex);
    if (renderTreeNode.nodeType(descendantNode) === NodeType.attribute) {
      applyAttribute(browserRendererId, componentId, newDomElement, descendantNode, descendantIndex);
    } else {
      // As soon as we see a non-attribute child, all the subsequent child nodes are
      // not attributes, so bail out and insert the remnants recursively
      insertNodeRange(browserRendererId, componentId, newDomElement, tree, descendantIndex, descendantsEndIndex);
      break;
    }
  }
}

function applyAttribute(browserRendererId: number, componentId: number, toDomElement: Element, attributeNode: RenderTreeNodePointer, attributeNodeIndex: number) {
  const attributeName = renderTreeNode.attributeName(attributeNode);

  switch (attributeName) {
    case 'onclick':
      toDomElement.addEventListener('click', () => raiseEvent(browserRendererId, componentId, attributeNodeIndex, 'mouse', { Type: 'click' }));
      break;
    case 'onkeypress':
      toDomElement.addEventListener('keypress', evt => {
        // This does not account for special keys nor cross-browser differences. So far it's
        // just to establish that we can pass parameters when raising events.
        // We use C#-style PascalCase on the eventInfo to simplify deserialization, but this could
        // change if we introduced a richer JSON library on the .NET side.
        raiseEvent(browserRendererId, componentId, attributeNodeIndex, 'keyboard', { Type: evt.type, Key: (evt as any).key });
      });
      break;
    default:
      // Treat as a regular string-valued attribute
      toDomElement.setAttribute(
        attributeName,
        renderTreeNode.attributeValue(attributeNode)
      );
      break;
  }
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

function insertText(intoDomElement: Element, textNode: RenderTreeNodePointer) {
  const textContent = renderTreeNode.textContent(textNode);
  const newDomTextNode = document.createTextNode(textContent);
  intoDomElement.appendChild(newDomTextNode);
}

function clearElement(element: Element) {
  let childNode: Node | null;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}

type EventInfoType = 'mouse' | 'keyboard';
