import { System_Object, System_String, System_Array, MethodHandle, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
import { getTreeNodePtr, renderTreeNode, NodeType, RenderTreeNodePointer } from './RenderTreeNode';
import { RenderTreeEditPointer } from './RenderTreeEdit';
import { renderComponentArgs, RenderComponentArgsPointer } from './RenderComponentArgs';
import { BrowserRenderer } from './BrowserRenderer';

type BrowserRendererRegistry = { [browserRendererId: number]: BrowserRenderer };
const browserRenderers: BrowserRendererRegistry = {};

export function attachComponentToElement(browserRendererId: number, elementSelector: System_String, componentId: number) {
  const elementSelectorJs = platform.toJavaScriptString(elementSelector);
  const element = document.querySelector(elementSelectorJs);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelectorJs}'.`);
  }

  let browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    browserRenderer = browserRenderers[browserRendererId] = new BrowserRenderer(browserRendererId);
  }
  browserRenderer.attachComponentToElement(componentId, element);
  clearElement(element);
}

export function renderRenderTree(args: RenderComponentArgsPointer) {
  const browserRendererId = renderComponentArgs.browserRendererId(args);
  const browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    throw new Error(`There is no browser renderer with ID ${browserRendererId}.`);
  }

  const componentId = renderComponentArgs.componentId(args);
  const edits = renderComponentArgs.renderTreeEdits(args);
  const editsLength = renderComponentArgs.renderTreeEditsLength(args);
  const tree = renderComponentArgs.renderTree(args);

  browserRenderer.updateComponent(componentId, edits, editsLength, tree);
}

function clearElement(element: Element) {
  let childNode: Node | null;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}
