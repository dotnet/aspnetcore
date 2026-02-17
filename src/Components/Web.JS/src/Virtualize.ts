// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export const Virtualize = {
  init,
  dispose,
};

const dispatcherObserversByDotNetIdPropname = Symbol();
const THROTTLE_MS = 50;

function findClosestScrollContainer(element: HTMLElement | null): HTMLElement | null {
  // If we recurse up as far as body or the document root, return null so that the
  // IntersectionObserver observes intersection with the top-level scroll viewport
  // instead of the with body/documentElement which can be arbitrarily tall.
  // See https://github.com/dotnet/aspnetcore/issues/37659 for more about what this fixes.
  if (!element || element === document.body || element === document.documentElement) {
    return null;
  }

  const style = getComputedStyle(element);

  if (style.overflowY !== 'visible') {
    return element;
  }

  return findClosestScrollContainer(element.parentElement);
}

function getScaleFactor(element: HTMLElement): number {
  // Use the ratio of getBoundingClientRect().height to offsetHeight to detect
  // cumulative CSS scaling (transform, zoom, scale) from all ancestors.
  // This is O(1) and handles all scaling types automatically.
  // Note: Both values exclude margin, so this ratio is margin-safe.
  if (element.offsetHeight === 0) {
    return 1;
  }
  const scale = element.getBoundingClientRect().height / element.offsetHeight;
  if (!Number.isFinite(scale) || scale <= 0) {
    return 1;
  }
  return scale;
}

interface MeasurementResult {
  heights: number[];
  scaleFactor: number;
}

function measureRenderedItems(
  spacerBefore: HTMLElement,
  spacerAfter: HTMLElement
): MeasurementResult {
  const container = spacerBefore.parentElement;
  if (!container) {
    return { heights: [], scaleFactor: getScaleFactor(spacerBefore) };
  }

  const items = container.querySelectorAll<HTMLElement>('[data-virtualize-item]');
  if (items.length === 0) {
    return { heights: [], scaleFactor: getScaleFactor(spacerBefore) };
  }

  // Get scale factor from the first item (all items share the same ancestors)
  const scaleFactor = getScaleFactor(items[0]);
  const heights: number[] = [];

  items.forEach(item => {
    const rect = item.getBoundingClientRect();
    heights.push(rect.height / scaleFactor);
  });

  return { heights, scaleFactor };
}

function init(dotNetHelper: DotNet.DotNetObject, spacerBefore: HTMLElement, spacerAfter: HTMLElement, rootMargin = 50): void {
  // Overflow anchoring can cause an ongoing scroll loop, because when we resize the spacers, the browser
  // would update the scroll position to compensate. Then the spacer would remain visible and we'd keep on
  // trying to resize it.
  const scrollContainer = findClosestScrollContainer(spacerBefore);
  (scrollContainer || document.documentElement).style.overflowAnchor = 'none';

  const rangeBetweenSpacers = document.createRange();

  if (isValidTableElement(spacerAfter.parentElement)) {
    spacerBefore.style.display = 'table-row';
    spacerAfter.style.display = 'table-row';
  }

  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: scrollContainer,
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(spacerBefore);
  intersectionObserver.observe(spacerAfter);

  const mutationObserverBefore = createSpacerMutationObserver(spacerBefore);
  const mutationObserverAfter = createSpacerMutationObserver(spacerAfter);

  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  observersByDotNetObjectId[id] = {
    intersectionObserver,
    mutationObserverBefore,
    mutationObserverAfter,
    onDispose: () => {
      if (callbackTimeout) {
        clearTimeout(callbackTimeout);
        callbackTimeout = null;
      }
      pendingCallbacks.clear();
    },
  };

  function createSpacerMutationObserver(spacer: HTMLElement): MutationObserver {
    // Without the use of thresholds, IntersectionObserver only detects binary changes in visibility,
    // so if a spacer gets resized but remains visible, no additional callbacks will occur. By unobserving
    // and reobserving spacers when they get resized, the intersection callback will re-run if they remain visible.
    const observerOptions = { attributes: true };
    const mutationObserver = new MutationObserver((mutations: MutationRecord[], observer: MutationObserver): void => {
      if (isValidTableElement(spacer.parentElement)) {
        observer.disconnect();
        spacer.style.display = 'table-row';
        observer.observe(spacer, observerOptions);
      }

      intersectionObserver.unobserve(spacer);
      intersectionObserver.observe(spacer);
    });

    mutationObserver.observe(spacer, observerOptions);

    return mutationObserver;
  }

  function flushPendingCallbacks(): void {
    if (pendingCallbacks.size === 0) return;
    const entries = Array.from(pendingCallbacks.values());
    pendingCallbacks.clear();
    processIntersectionEntries(entries);
  }

  function intersectionCallback(entries: IntersectionObserverEntry[]): void {
    entries.forEach(entry => pendingCallbacks.set(entry.target, entry));
    
    if (!callbackTimeout) {
      flushPendingCallbacks();
      
      callbackTimeout = setTimeout(() => {
        callbackTimeout = null;
        flushPendingCallbacks();
      }, THROTTLE_MS);
    }
  }

  function processIntersectionEntries(entries: IntersectionObserverEntry[]): void {
    entries.forEach((entry): void => {
      if (!entry.isIntersecting) {
        return;
      }

      const { heights: measurements, scaleFactor } = measureRenderedItems(spacerBefore, spacerAfter);

      // To compute the ItemSize, work out the separation between the two spacers. We can't just measure an individual element
      // because each conceptual item could be made from multiple elements. Using getBoundingClientRect allows for the size to be
      // a fractional value. It's important not to add or subtract any such fractional values (e.g., to subtract the 'top' of
      // one item from the 'bottom' of another to get the distance between them) because floating point errors would cause
      // scrolling glitches.
      rangeBetweenSpacers.setStartAfter(spacerBefore);
      rangeBetweenSpacers.setEndBefore(spacerAfter);
      const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height / scaleFactor;
      const containerSize = (entry.rootBounds?.height ?? 0) / scaleFactor;

      if (entry.target === spacerBefore) {
        const spacerSize = (entry.intersectionRect.top - entry.boundingClientRect.top) / scaleFactor;
        dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', spacerSize, spacerSeparation, containerSize, measurements);
      } else if (entry.target === spacerAfter && spacerAfter.offsetHeight > 0) {
        // When we first start up, both the "before" and "after" spacers will be visible, but it's only relevant to raise a
        // single event to load the initial data. To avoid raising two events, skip the one for the "after" spacer if we know
        // it's meaningless to talk about any overlap into it.
        const spacerSize = (entry.boundingClientRect.bottom - entry.intersectionRect.bottom) / scaleFactor;
        dotNetHelper.invokeMethodAsync('OnSpacerAfterVisible', spacerSize, spacerSeparation, containerSize, measurements);
      }
    });
  }

  function isValidTableElement(element: HTMLElement | null): boolean {
    if (element === null) {
      return false;
    }

    return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
      || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
  }
}

function getObserversMapEntry(dotNetHelper: DotNet.DotNetObject): { observersByDotNetObjectId: {[id: number]: any }, id: number } {
  const dotNetHelperDispatcher = dotNetHelper['_callDispatcher'];
  const dotNetHelperId = dotNetHelper['_id'];
  dotNetHelperDispatcher[dispatcherObserversByDotNetIdPropname] ??= { };

  return {
    observersByDotNetObjectId: dotNetHelperDispatcher[dispatcherObserversByDotNetIdPropname],
    id: dotNetHelperId,
  };
}

function dispose(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const observers = observersByDotNetObjectId[id];

  if (observers) {
    observers.intersectionObserver.disconnect();
    observers.mutationObserverBefore.disconnect();
    observers.mutationObserverAfter.disconnect();
    observers.onDispose?.();

    dotNetHelper.dispose();

    delete observersByDotNetObjectId[id];
  }
}
