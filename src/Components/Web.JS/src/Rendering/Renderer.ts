/* eslint-disable @typescript-eslint/camelcase */
import '../Platform/Platform';
import '../Environment';
import { RenderBatch } from './RenderBatch/RenderBatch';
import { BrowserRenderer } from './BrowserRenderer';
import { toLogicalElement, LogicalElement } from './LogicalElements';

interface BrowserRendererRegistry {
  [browserRendererId: number]: BrowserRenderer;
}
const browserRenderers: BrowserRendererRegistry = {};
let shouldResetScrollAfterNextBatch = false;

export function attachRootComponentToLogicalElement(browserRendererId: number, logicalElement: LogicalElement, componentId: number): void {

  let browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    browserRenderer = browserRenderers[browserRendererId] = new BrowserRenderer(browserRendererId);
  }

  browserRenderer.attachRootComponentToLogicalElement(componentId, logicalElement);
}

export function attachRootComponentToElement(browserRendererId: number, elementSelector: string, componentId: number): void {

  const element = document.querySelector(elementSelector);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelector}'.`);
  }

  // 'allowExistingContents' to keep any prerendered content until we do the first client-side render
  attachRootComponentToLogicalElement(browserRendererId, toLogicalElement(element, /* allow existing contents */ true), componentId);
}

export function renderBatch(browserRendererId: number, batch: RenderBatch): void {
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

  resetScrollIfNeeded();
}

export function resetScrollAfterNextBatch() {
  shouldResetScrollAfterNextBatch = true;
}

function resetScrollIfNeeded() {
  if (shouldResetScrollAfterNextBatch) {
    shouldResetScrollAfterNextBatch = false;

    // This assumes the scroller is on the window itself. There isn't a general way to know
    // if some other element is playing the role of the primary scroll region.
    window.scrollTo && window.scrollTo(0, 0);
  }
}
