import { registerFunction } from '../RegisteredFunction';
import { System_String, System_Array } from '../Platform/Platform';
import { platform } from '../Environment';
import { getTreeNodePtr, uiTreeNode, NodeType, UITreeNodePointer } from './UITreeNode';

// TODO: Instead of associating components to parent elements, associate them with a
// start/end node, so that components don't have to be enclosed in a wrapper
// TODO: To avoid leaking memory, automatically remove entries from this dict as soon
// as the corresponding DOM nodes are removed (or maybe when the associated component
// is disposed, assuming we can guarantee that always happens).
const componentIdToParentElement: { [componentId: string]: Element } = {};

registerFunction('_blazorAttachComponentToElement', attachComponentToElement);
registerFunction('_blazorRender', renderUITree);

function attachComponentToElement(elementSelector: System_String, componentId: System_String) {
  const elementSelectorJs = platform.toJavaScriptString(elementSelector);
  const element = document.querySelector(elementSelectorJs);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelectorJs}'.`);
  }

  clearElement(element);

  const componentIdJs = platform.toJavaScriptString(componentId);
  componentIdToParentElement[componentIdJs] = element;
}

function renderUITree(componentId: System_String, tree: System_Array, treeLength: number) {
  const componentIdJs = platform.toJavaScriptString(componentId);
  const element = componentIdToParentElement[componentIdJs];
  if (!element) {
    throw new Error(`No element is currently associated with component ${componentIdJs}`);
  }

  insertNodeRange(element, tree, 0, treeLength - 1);
}

function insertNodeRange(intoDomElement: Element, tree: System_Array, startIndex: number, endIndex: number) {
  for (let index = startIndex; index <= endIndex; index++) {
    const node = getTreeNodePtr(tree, index);
    insertNode(intoDomElement, tree, node, index);

    // Skip over any descendants, since they are already dealt with recursively
    const descendantsEndIndex = uiTreeNode.descendantsEndIndex(node);
    if (descendantsEndIndex > 0) {
      index = descendantsEndIndex;
    }
  }
}

function insertNode(intoDomElement: Element, tree: System_Array, node: UITreeNodePointer, nodeIndex: number) {
  const nodeType = uiTreeNode.nodeType(node);
  switch (nodeType) {
    case NodeType.element:
      insertElement(intoDomElement, tree, node, nodeIndex);
      break;
    case NodeType.text:
      insertText(intoDomElement, node);
      break;
    case NodeType.attribute:
      throw new Error('Attribute nodes should only be present as leading children of element nodes.');
    default:
      const unknownType: never = nodeType; // Compile-time verification that the switch was exhaustive
      throw new Error(`Unknown node type: ${ unknownType }`);
  }
}

function insertElement(intoDomElement: Element, tree: System_Array, elementNode: UITreeNodePointer, elementNodeIndex: number) {
  const tagName = uiTreeNode.elementName(elementNode);
  const newDomElement = document.createElement(tagName);
  intoDomElement.appendChild(newDomElement);

  // Apply attributes
  const descendantsEndIndex = uiTreeNode.descendantsEndIndex(elementNode);
  for (let descendantIndex = elementNodeIndex + 1; descendantIndex <= descendantsEndIndex; descendantIndex++) {
    const descendantNode = getTreeNodePtr(tree, descendantIndex);
    if (uiTreeNode.nodeType(descendantNode) === NodeType.attribute) {
      applyAttribute(newDomElement, descendantNode);
    } else {
      // As soon as we see a non-attribute child, all the subsequent child nodes are
      // not attributes, so bail out and insert the remnants recursively
      insertNodeRange(newDomElement, tree, descendantIndex, descendantsEndIndex);
      break;
    }
  }
}

function applyAttribute(toDomElement: Element, attributeNode: UITreeNodePointer) {
  toDomElement.setAttribute(
    uiTreeNode.attributeName(attributeNode),
    uiTreeNode.attributeValue(attributeNode)
  );
}

function insertText(intoDomElement: Element, textNode: UITreeNodePointer) {
  const textContent = uiTreeNode.textContent(textNode);
  const newDomTextNode = document.createTextNode(textContent);
  intoDomElement.appendChild(newDomTextNode);
}

function clearElement(element: Element) {
  let childNode: Node;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}
