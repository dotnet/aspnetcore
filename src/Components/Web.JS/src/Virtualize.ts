// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export const Virtualize = {
  init,
  dispose,
  scrollToBottom,
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

interface MeasurementResult {
  heights: number[];
  scaleFactor: number;
}

function measureRenderedItems(spacerBefore: HTMLElement, spacerAfter: HTMLElement): MeasurementResult {
  const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);
  const items = spacerBefore.parentElement
    ?.querySelectorAll<HTMLElement>('[data-virtualize-item]');

  if (!items || items.length === 0) {
    return { heights: [], scaleFactor };
  }

  const heights = Array.from(
    items,
    item => item.getBoundingClientRect().height / scaleFactor,
  );
  return { heights, scaleFactor };
}

function init(dotNetHelper: DotNet.DotNetObject, spacerBefore: HTMLElement, spacerAfter: HTMLElement, rootMargin = 50): void {
  // Overflow anchoring can cause an ongoing scroll loop, because when we resize the spacers, the browser
  // would update the scroll position to compensate. Then the spacer would remain visible and we'd keep on
  // trying to resize it.
  const scrollContainer = findClosestScrollContainer(spacerBefore);
  const scrollElement = scrollContainer || document.documentElement;
  scrollElement.style.overflowAnchor = 'none';

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

  let snapToBottom = false;

  const mutationObserverBefore = createSpacerMutationObserver(spacerBefore);
  const mutationObserverAfter = createSpacerMutationObserver(spacerAfter);

  // keeps scroll pinned to bottom after DOM changes if spacerAfter is collapsed
  const containerObserver = new MutationObserver((): void => {
    if (spacerAfter.offsetHeight === 0) {
      scrollElement.scrollTop = scrollElement.scrollHeight;
    } else {
      setSnapToBottom(false);
    }
  });

  function setSnapToBottom(value: boolean): void {
    if (value === snapToBottom) {
      return;
    }
    snapToBottom = value;
    if (value && spacerBefore.parentElement) {
      containerObserver.observe(spacerBefore.parentElement, { childList: true, subtree: true, attributes: true });
    } else if (!value) {
      containerObserver.disconnect();
    }
  }

  let lastSpacerAfterScrollTop: number | null = null;
  let lastSpacerBeforeScrollTop: number | null = null;

  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  observersByDotNetObjectId[id] = {
    intersectionObserver,
    mutationObserverBefore,
    mutationObserverAfter,
    containerObserver,
    scrollElement,
    setSnapToBottom,
    onDispose: () => {
      setSnapToBottom(false);
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
    const intersectingEntries: IntersectionObserverEntry[] = [];

    for (const entry of entries) {
      if (entry.isIntersecting) {
        intersectingEntries.push(entry);
        if (entry.target === spacerAfter && spacerAfter.offsetHeight > 0) {
          lastSpacerAfterScrollTop = scrollElement.scrollTop;
        } else if (entry.target === spacerBefore && spacerBefore.offsetHeight > 0) {
          lastSpacerBeforeScrollTop = scrollElement.scrollTop;
        }
      } else if (entry.target === spacerAfter) {
        if (lastSpacerAfterScrollTop !== null
            && Math.abs(scrollElement.scrollTop - lastSpacerAfterScrollTop) < 1
            && spacerAfter.offsetHeight > 0) {
          scrollElement.scrollTop = scrollElement.scrollHeight;
        }
        lastSpacerAfterScrollTop = null;
      } else if (entry.target === spacerBefore) {
        if (lastSpacerBeforeScrollTop !== null
            && Math.abs(scrollElement.scrollTop - lastSpacerBeforeScrollTop) < 1
            && spacerBefore.offsetHeight > 0) {
          scrollElement.scrollTop = 0;
        }
        lastSpacerBeforeScrollTop = null;
      }
    }

    if (intersectingEntries.length === 0) {
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

    intersectingEntries.forEach((entry): void => {
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

function scrollToBottom(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  if (entry) {
    entry.scrollElement.scrollTop = entry.scrollElement.scrollHeight;
    entry.setSnapToBottom?.(true);
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
