import { System_Object, System_String, System_Array, MethodHandle, Pointer } from '../Platform/Platform';
import { platform } from '../Environment';
import { RenderBatch } from './RenderBatch/RenderBatch';
import { BrowserRenderer } from './BrowserRenderer';

type BrowserRendererRegistry = { [browserRendererId: number]: BrowserRenderer };
const browserRenderers: BrowserRendererRegistry = {};

export function attachRootComponentToElement(browserRendererId: number, elementSelector: string, componentId: number) {
  const element = document.querySelector(elementSelector);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelector}'.`);
  }

  let browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    browserRenderer = browserRenderers[browserRendererId] = new BrowserRenderer(browserRendererId);
  }
  browserRenderer.attachRootComponentToElement(componentId, element);
}

export function renderBatch(browserRendererId: number, batch: RenderBatch) {
  const browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    throw new Error(`There is no browser renderer with ID ${browserRendererId}.`);
  }

  const arrayRangeReader = batch.arrayRangeReader;
  const updatedComponentsRange = batch.updatedComponents();
  const updatedComponentsValues = arrayRangeReader.values(updatedComponentsRange);
  const updatedComponentsLength = arrayRangeReader.count(updatedComponentsRange);
  const referenceFrames = batch.referenceFrames();
  const referenceFramesValues = arrayRangeReader.values(referenceFrames);
  const diffReader = batch.diffReader;

  for (let i = 0; i < updatedComponentsLength; i++) {
    const diff = batch.updatedComponentsEntry(updatedComponentsValues, i);
    const componentId = diffReader.componentId(diff);
    const edits = diffReader.edits(diff);
    browserRenderer.updateComponent(batch, componentId, edits, referenceFramesValues);
  }

  const disposedComponentIdsRange = batch.disposedComponentIds();
  const disposedComponentIdsValues = arrayRangeReader.values(disposedComponentIdsRange);
  const disposedComponentIdsLength = arrayRangeReader.count(disposedComponentIdsRange);
  for (let i = 0; i < disposedComponentIdsLength; i++) {
    const componentId = batch.disposedComponentIdsEntry(disposedComponentIdsValues, i);
    browserRenderer.disposeComponent(componentId);
  }

  const disposedEventHandlerIdsRange = batch.disposedEventHandlerIds();
  const disposedEventHandlerIdsValues = arrayRangeReader.values(disposedEventHandlerIdsRange);
  const disposedEventHandlerIdsLength = arrayRangeReader.count(disposedEventHandlerIdsRange);
  for (let i = 0; i < disposedEventHandlerIdsLength; i++) {
    const eventHandlerId = batch.disposedEventHandlerIdsEntry(disposedEventHandlerIdsValues, i);
    browserRenderer.disposeEventHandler(eventHandlerId);
  }
}
