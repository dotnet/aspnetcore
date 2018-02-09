import { System_Object, System_String, System_Array, MethodHandle, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
import { renderBatch as renderBatchStruct, arrayRange, arraySegment, renderTreeDiffStructLength, renderTreeDiff, RenderBatchPointer, RenderTreeDiffPointer } from './RenderBatch';
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
  const referenceFramesStruct = renderBatchStruct.referenceFrames(batch);
  const referenceFrames = arrayRange.array(referenceFramesStruct);

  for (let i = 0; i < updatedComponentsLength; i++) {
    const diff = platform.getArrayEntryPtr(updatedComponentsArray, i, renderTreeDiffStructLength);
    const componentId = renderTreeDiff.componentId(diff);

    const editsArraySegment = renderTreeDiff.edits(diff);
    const edits = arraySegment.array(editsArraySegment);
    const editsOffset = arraySegment.offset(editsArraySegment);
    const editsLength = arraySegment.count(editsArraySegment);

    browserRenderer.updateComponent(componentId, edits, editsOffset, editsLength, referenceFrames);
  }

  const disposedComponentIds = renderBatchStruct.disposedComponentIds(batch);
  const disposedComponentIdsLength = arrayRange.count(disposedComponentIds);
  const disposedComponentIdsArray = arrayRange.array(disposedComponentIds);
  for (let i = 0; i < disposedComponentIdsLength; i++) {
    const componentIdPtr = platform.getArrayEntryPtr(disposedComponentIdsArray, i, 4);
    const componentId = platform.readInt32Field(componentIdPtr);
    browserRenderer.disposeComponent(componentId);
  }
}

function clearElement(element: Element) {
  let childNode: Node | null;
  while (childNode = element.firstChild) {
    element.removeChild(childNode);
  }
}
