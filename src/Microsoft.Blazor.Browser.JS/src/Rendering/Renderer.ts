import { registerFunction } from '../RegisteredFunction';
import { System_String, System_Array } from '../Platform/Platform';
import { platform } from '../Environment';
import { getTreeNodePtr, uiTreeNode, NodeType, UITreeNodePointer } from './UITreeNode';

registerFunction('_blazorRender', renderUITree);

function renderUITree(elementSelector: System_String, tree: System_Array, treeLength: number) {
  const elementSelectorJs = platform.toJavaScriptString(elementSelector);
  const element = document.querySelector(elementSelectorJs);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${ elementSelectorJs }'.`);
  }

  clearElement(element);
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
    default:
      const unknownType: never = nodeType; // Compile-time verification that the switch was exhaustive
      throw new Error(`Unknown node type: ${ unknownType }`);
  }
}

function insertElement(intoDomElement: Element, tree: System_Array, elementNode: UITreeNodePointer, elementNodeIndex: number) {
  const tagName = uiTreeNode.elementName(elementNode);
  const newDomElement = document.createElement(tagName);
  intoDomElement.appendChild(newDomElement);

  // Recursively insert children
  const descendantsEndIndex = uiTreeNode.descendantsEndIndex(elementNode);
  insertNodeRange(newDomElement, tree, elementNodeIndex + 1, descendantsEndIndex);
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
