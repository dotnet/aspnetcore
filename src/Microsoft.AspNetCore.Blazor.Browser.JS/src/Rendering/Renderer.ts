import { System_Object, System_String, System_Array, MethodHandle, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
import { getTreeNodePtr, renderTreeNode, NodeType, RenderTreeNodePointer } from './RenderTreeNode';
import { RenderTreeEditPointer } from './RenderTreeEdit';
import { renderBatch as renderBatchStruct, arrayRange, renderTreeDiffStructLength, renderTreeDiff, RenderBatchPointer, RenderTreeDiffPointer } from './RenderBatch';
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

export function renderBatch(browserRendererId: number, batch: RenderBatchPointer) {
  const browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    throw new Error(`There is no browser renderer with ID ${browserRendererId}.`);
  }
  
  const updatedComponents = renderBatchStruct.updatedComponents(batch);
  const updatedComponentsLength = arrayRange.count(updatedComponents);
  const updatedComponentsArray = arrayRange.array(updatedComponents);
  for (var i = 0; i < updatedComponentsLength; i++) {
    const diff = platform.getArrayEntryPtr(updatedComponentsArray, i, renderTreeDiffStructLength);
    const componentId = renderTreeDiff.componentId(diff);

    const editsArrayRange = renderTreeDiff.edits(diff);
    const currentStateArrayRange = renderTreeDiff.currentState(diff);

    const edits = arrayRange.array(editsArrayRange);
    const editsLength = arrayRange.count(editsArrayRange);
    const tree = arrayRange.array(currentStateArrayRange);
    browserRenderer.updateComponent(componentId, edits, editsLength, tree);
  }
}

function clearElement(element: Element) {
  let childNode: Node | null;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}
