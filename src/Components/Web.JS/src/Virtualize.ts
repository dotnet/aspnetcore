// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export const Virtualize = {
  init,
  dispose,
  scrollToBottom,
  refreshObservers,
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

  if (style.overflowY !== 'visible' && style.overflowY !== 'hidden' && style.overflowY !== 'clip') {
    return element;
  }

  return findClosestScrollContainer(element.parentElement);
}

function getScaleFactor(spacerBefore: HTMLElement, spacerAfter: HTMLElement): number {
  const el = spacerBefore.offsetHeight > 0 ? spacerBefore
    : spacerAfter.offsetHeight > 0 ? spacerAfter
    : null;
  if (!el) {
    return 1;
  }
  const scale = el.getBoundingClientRect().height / el.offsetHeight;
  return (Number.isFinite(scale) && scale > 0) ? scale : 1;
}

function init(dotNetHelper: DotNet.DotNetObject, spacerBefore: HTMLElement, spacerAfter: HTMLElement, rootMargin = 50): void {
  // If the component was disposed before the JS interop call completed, the element references may be null
  // or the elements may have been disconnected from the DOM. Return early to avoid errors.
  if (!spacerBefore || !spacerAfter || !spacerBefore.isConnected || !spacerAfter.isConnected) {
    return;
  }

  const scrollContainer = findClosestScrollContainer(spacerBefore);
  const scrollElement = scrollContainer || document.documentElement;
  const isTable = isValidTableElement(spacerAfter.parentElement);
  const supportsAnchor = CSS.supports('overflow-anchor', 'auto');
  const useNativeAnchoring = !isTable && supportsAnchor;

  const rangeBetweenSpacers = document.createRange();

  if (isTable) {
    spacerBefore.style.display = 'table-row';
    spacerAfter.style.display = 'table-row';
  }

  if (useNativeAnchoring) {
    // Applied to rendered items - keeps viewport stable when spacer heights change.
    spacerBefore.style.overflowAnchor = 'none';
    spacerAfter.style.overflowAnchor = 'none';
  } else {
    // Manual compensation path for tables and browsers without native anchoring.
    scrollElement.style.overflowAnchor = 'none';
  }

  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: scrollContainer,
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(spacerBefore);
  intersectionObserver.observe(spacerAfter);

  const anchoredItems: Map<Element, number> = new Map();
  let scrollTriggeredRender = false;

  function getObservedHeight(entry: ResizeObserverEntry): number {
    return entry.borderBoxSize?.[0]?.blockSize ?? entry.contentRect.height;
  }

  // ResizeObserver roles:
  //  1. Always observes both spacers so that when a spacer resizes we re-trigger the
  //     IntersectionObserver — which otherwise won't fire again for an element that is already visible.
  //  2. Viewport anchoring — compensates scroll position when content above the viewport resizes.
  //     When native anchoring is available (non-table + supported browser), the browser handles this.
  //     Otherwise (tables, Safari), we use manual compensation via item height tracking.
  const resizeObserver = new ResizeObserver((entries: ResizeObserverEntry[]): void => {
    // 1. Re-trigger IntersectionObserver for spacer resizes.
    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        const spacer = entry.target as HTMLElement;
        if (spacer.isConnected) {
          intersectionObserver.unobserve(spacer);
          intersectionObserver.observe(spacer);
        }
      }
    }

    // 2. Viewport anchoring: compensate scroll for above-viewport item resizes.
    let scrollDelta = 0;
    const containerTop = scrollContainer
      ? scrollContainer.getBoundingClientRect().top
      : 0;

    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        // Skip spacer entries — spacers resize during normal scroll-driven
        // rendering. Compensating here would undo normal scrolling.
        continue;
      }

      if (entry.target.isConnected) {
        const el = entry.target as HTMLElement;
        const oldHeight = anchoredItems.get(el);
        const newHeight = getObservedHeight(entry);
        anchoredItems.set(el, newHeight);

        if (oldHeight !== undefined && oldHeight !== newHeight) {
          // Compensate if the element is above the viewport (fully or partially).
          if (el.getBoundingClientRect().top < containerTop) {
            scrollDelta += (newHeight - oldHeight);
          }
        }
      }
    }

    if (scrollDelta !== 0 && scrollElement.scrollTop > 0) {
      scrollElement.scrollTop += scrollDelta;
    }
  });

  // Always observe both spacers for the IntersectionObserver re-trigger.
  resizeObserver.observe(spacerBefore);
  resizeObserver.observe(spacerAfter);

  function refreshObservedElements(): void {
    // C# style updates overwrite the entire style attribute. Re-apply what we need.
    if (isTable) {
      spacerBefore.style.display = 'table-row';
      spacerAfter.style.display = 'table-row';
    }

    if (useNativeAnchoring) {
      // Re-apply overflow-anchor: none on spacers after C# re-renders.
      spacerBefore.style.overflowAnchor = 'none';
      spacerAfter.style.overflowAnchor = 'none';
    }

    // Ensure spacers are always observed (idempotent).
    resizeObserver.observe(spacerBefore);
    resizeObserver.observe(spacerAfter);

    // - Native anchoring: browser handles above-viewport resizes automatically.
    // - Manual compensation: observe items on data-triggered renders to compensate.
    if (useNativeAnchoring || scrollTriggeredRender) {
      scrollTriggeredRender = false;
      return;
    }
    scrollTriggeredRender = false;

    // Observe all rendered items for viewport anchoring. When an item
    // resizes above the viewport, the ResizeObserver callback compensates scrollTop.
    const currentItems = new Set<Element>();
    for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
      resizeObserver.observe(el);
      currentItems.add(el);
    }

    // Unobserve items removed during re-render and clean up height tracking.
    for (const [el] of anchoredItems) {
      if (!currentItems.has(el)) {
        resizeObserver.unobserve(el);
        anchoredItems.delete(el);
      }
    }
  }

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;

  observersByDotNetObjectId[id] = {
    intersectionObserver,
    resizeObserver,
    refreshObservedElements,
    scrollElement,
    onDispose: () => {
      anchoredItems.clear();
      resizeObserver.disconnect();
      if (callbackTimeout) {
        clearTimeout(callbackTimeout);
        callbackTimeout = null;
      }
      pendingCallbacks.clear();
    },
  };

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
    // Check if the spacers are still in the DOM. They may have been removed if the component was disposed.
    if (!spacerBefore.isConnected || !spacerAfter.isConnected) {
      return;
    }

    const intersectingEntries = entries.filter(entry => entry.isIntersecting);

    if (intersectingEntries.length === 0) {
      return;
    }

    const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);

    rangeBetweenSpacers.setStartAfter(spacerBefore);
    rangeBetweenSpacers.setEndBefore(spacerAfter);
    const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height / scaleFactor;

    intersectingEntries.forEach((entry): void => {
      const containerSize = (entry.rootBounds?.height ?? 0) / scaleFactor;

      // Mark the upcoming render as scroll-triggered so refreshObservedElements
      // skips item observation for tables (avoids layout interference drift).
      scrollTriggeredRender = true;

      if (entry.target === spacerBefore) {
        const spacerSize = (entry.intersectionRect.top - entry.boundingClientRect.top) / scaleFactor;
        dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', spacerSize, spacerSeparation, containerSize);
      } else if (entry.target === spacerAfter && spacerAfter.offsetHeight > 0) {
        // When we first start up, both the "before" and "after" spacers will be visible, but it's only relevant to raise a
        // single event to load the initial data. To avoid raising two events, skip the one for the "after" spacer if we know
        // it's meaningless to talk about any overlap into it.
        const spacerSize = (entry.boundingClientRect.bottom - entry.intersectionRect.bottom) / scaleFactor;
        dotNetHelper.invokeMethodAsync('OnSpacerAfterVisible', spacerSize, spacerSeparation, containerSize);
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

function scrollToBottom(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  if (entry) {
    entry.scrollElement.scrollTop = entry.scrollElement.scrollHeight;
  }
}

function refreshObservers(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  entry?.refreshObservedElements?.();
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
    observers.resizeObserver?.disconnect();
    observers.onDispose?.();

    delete observersByDotNetObjectId[id];
  }

  // Always dispose the dotNetHelper to release the DotNetObjectReference,
  // even if init() returned early and no observers were created.
  dotNetHelper.dispose();
}
