// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import '../Platform/Platform';
import '../Environment';
import { RenderBatch } from './RenderBatch/RenderBatch';
import { BrowserRenderer } from './BrowserRenderer';
import { toLogicalElement, LogicalElement } from './LogicalElements';
import { getAndRemovePendingRootComponentContainer } from './JSRootComponents';

interface BrowserRendererRegistry {
  [browserRendererId: number]: BrowserRenderer;
}
const browserRenderers: BrowserRendererRegistry = {};
let shouldResetScrollAfterNextBatch = false;
let elementToScrollTo : string | null = null;
let scrollToElementTimeout : NodeJS.Timeout;

export function attachRootComponentToLogicalElement(browserRendererId: number, logicalElement: LogicalElement, componentId: number, appendContent: boolean): void {
  let browserRenderer = browserRenderers[browserRendererId];
  if (!browserRenderer) {
    browserRenderer = new BrowserRenderer(browserRendererId);
    browserRenderers[browserRendererId] = browserRenderer;
  }

  browserRenderer.attachRootComponentToLogicalElement(componentId, logicalElement, appendContent);
}

export function attachRootComponentToElement(elementSelector: string, componentId: number, browserRendererId?: number): void {
  const afterElementSelector = '::after';
  const beforeElementSelector = '::before';
  let appendContent = false;

  if (elementSelector.endsWith(afterElementSelector)) {
    elementSelector = elementSelector.slice(0, -afterElementSelector.length);
    appendContent = true;
  } else if (elementSelector.endsWith(beforeElementSelector)) {
    throw new Error(`The '${beforeElementSelector}' selector is not supported.`);
  }

  const element = getAndRemovePendingRootComponentContainer(elementSelector)
    || document.querySelector(elementSelector);
  if (!element) {
    throw new Error(`Could not find any element matching selector '${elementSelector}'.`);
  }

  // 'allowExistingContents' to keep any prerendered content until we do the first client-side render
  // Only client-side Blazor supplies a browser renderer ID
  attachRootComponentToLogicalElement(browserRendererId || 0, toLogicalElement(element, /* allow existing contents */ true), componentId, appendContent);
}

export function getRendererer(browserRendererId: number): BrowserRenderer {
  return browserRenderers[browserRendererId];
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

  if (elementToScrollTo && scrollToElement(elementToScrollTo)) {
    elementToScrollTo = null;
    clearTimeout(scrollToElementTimeout); 

    // We found the element on the page and we don't want to scroll to the top of the page on the next batch
    shouldResetScrollAfterNextBatch = false; 
  } else {
    resetScrollIfNeeded();
  }  
}

export function scrollToElement(identifier : string) : boolean {
  let element : HTMLElement | null = null;

  element = document.getElementById(identifier);
  
  if (!element) {
    let elements = document.getElementsByName(identifier);
    if (elements.length > 0) {
      element = elements[0];     
    }
  }
  
  if (element) {
    element.scrollIntoView();
    return true;
  }

  return false;
}

export function setTimeoutToScrollToElement(identifier : string) : void {
  if (scrollToElementTimeout) {
    clearTimeout(scrollToElementTimeout);
  }
  elementToScrollTo = identifier;
  
  scrollToElementTimeout = setTimeout( () => {
    elementToScrollTo = null;
  }, 3000);
}

export function resetScrollAfterNextBatch(): void {
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
