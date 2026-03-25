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

  let convergingElements = false;
  let convergenceItems: Set<Element> = new Set();

 // ResizeObserver roles:
  //  1. Always observes both spacers so that when a spacer resizes we re-trigger the
  //     IntersectionObserver — which otherwise won't fire again for an element that is already visible.
  //  2. For convergence (sticky-top/bottom) - observes elements for geometry changes, drives the scroll position.
  const resizeObserver = new ResizeObserver((entries: ResizeObserverEntry[]): void => {
    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        const spacer = entry.target as HTMLElement;
        if (spacer.isConnected) {
          intersectionObserver.unobserve(spacer);
          intersectionObserver.observe(spacer);
        }
      }
    }

    // Convergence logic: keep scroll pinned to top/bottom while items load.
    if (convergingToBottom || convergingToTop) {
      scrollElement.scrollTop = convergingToBottom ? scrollElement.scrollHeight : 0;
      const spacer = convergingToBottom ? spacerAfter : spacerBefore;
      if (spacer.offsetHeight === 0) {
        convergingToBottom = convergingToTop = false;
        stopConvergenceObserving();
      }
    } else if (convergingElements) {
      stopConvergenceObserving();
    }
  });

  // Always observe both spacers for the IntersectionObserver re-trigger.
  resizeObserver.observe(spacerBefore);
  resizeObserver.observe(spacerAfter);

  function refreshObservedElements(): void {
    // C# style updates overwrite the entire style attribute, losing display: table-row.
    // Re-apply it so spacers participate in table layout alongside bare <tr> items.
    if (isValidTableElement(spacerAfter.parentElement)) {
      spacerBefore.style.display = 'table-row';
      spacerAfter.style.display = 'table-row';
    }

    // Ensure spacers are always observed (idempotent).
    resizeObserver.observe(spacerBefore);
    resizeObserver.observe(spacerAfter);

    // During convergence, keep the observed element set in sync with the DOM.
    if (convergingElements) {
      const currentItems: Set<Element> = new Set();
      for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
        resizeObserver.observe(el);
        currentItems.add(el);
      }
      // Unobserve items removed during re-render.
      for (const el of convergenceItems) {
        if (!currentItems.has(el)) {
          resizeObserver.unobserve(el);
        }
      }
      convergenceItems = currentItems;
    }

    // Don't re-trigger IntersectionObserver here — ResizeObserver handles that
    // when spacers actually resize. Doing it on every render causes feedback loops.
  }

  function startConvergenceObserving(): void {
    if (convergingElements) return;
    convergingElements = true;
    for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
      resizeObserver.observe(el);
      convergenceItems.add(el);
    }
  }

  function stopConvergenceObserving(): void {
    if (!convergingElements) return;
    convergingElements = false;
    for (const el of convergenceItems) {
      resizeObserver.unobserve(el);
    }
    convergenceItems.clear();
  }

  let convergingToBottom = false;
  let convergingToTop = false;

  let pendingJumpToEnd = false;
  let pendingJumpToStart = false;

  const keydownTarget: EventTarget = scrollContainer || document;
  function handleJumpKeys(e: Event): void {
    const ke = e as KeyboardEvent;
    if (ke.key === 'End') {
      pendingJumpToEnd = true;
      pendingJumpToStart = false;
    } else if (ke.key === 'Home') {
      pendingJumpToStart = true;
      pendingJumpToEnd = false;
    }
  }
  keydownTarget.addEventListener('keydown', handleJumpKeys);

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;

  observersByDotNetObjectId[id] = {
    intersectionObserver,
    resizeObserver,
    refreshObservedElements,
    scrollElement,
    startConvergenceObserving,
    onDispose: () => {
      stopConvergenceObserving();
      resizeObserver.disconnect();
      keydownTarget.removeEventListener('keydown', handleJumpKeys);
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

  function onSpacerAfterVisible(): void {
    if (spacerAfter.offsetHeight === 0) {
      if (convergingToBottom) {
        convergingToBottom = false;
        stopConvergenceObserving();
      }
      return;
    }
    if (convergingToBottom) return;

    const atBottom = scrollElement.scrollTop + scrollElement.clientHeight >= scrollElement.scrollHeight - 1;
    if (!atBottom && !pendingJumpToEnd) return;

    convergingToBottom = true;
    startConvergenceObserving();
    if (pendingJumpToEnd) {
      scrollElement.scrollTop = scrollElement.scrollHeight;
      pendingJumpToEnd = false;
    }
  }

  function onSpacerBeforeVisible(): void {
    if (spacerBefore.offsetHeight === 0) {
      if (convergingToTop) {
        convergingToTop = false;
        stopConvergenceObserving();
      }
      return;
    }
    if (convergingToTop) return;

    const atTop = scrollElement.scrollTop < 1;
    if (!atTop && !pendingJumpToStart) return;

    convergingToTop = true;
    startConvergenceObserving();
    if (pendingJumpToStart) {
      scrollElement.scrollTop = 0;
      pendingJumpToStart = false;
    }
  }

  function processIntersectionEntries(entries: IntersectionObserverEntry[]): void {
    // Check if the spacers are still in the DOM. They may have been removed if the component was disposed.
    if (!spacerBefore.isConnected || !spacerAfter.isConnected) {
      return;
    }

    const intersectingEntries = entries.filter(entry => {
      if (entry.isIntersecting) {
        if (entry.target === spacerAfter) {
          onSpacerAfterVisible();
        } else if (entry.target === spacerBefore) {
          onSpacerBeforeVisible();
        }
        return true;
      }
      if (entry.target === spacerAfter && convergingToBottom && spacerAfter.offsetHeight > 0) {
        scrollElement.scrollTop = scrollElement.scrollHeight;
      } else if (entry.target === spacerBefore && convergingToTop && spacerBefore.offsetHeight > 0) {
        scrollElement.scrollTop = 0;
      }
      return false;
    });

    if (intersectingEntries.length === 0) {
      return;
    }

    const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);

    rangeBetweenSpacers.setStartAfter(spacerBefore);
    rangeBetweenSpacers.setEndBefore(spacerAfter);
    const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height / scaleFactor;

    intersectingEntries.forEach((entry): void => {
      const containerSize = (entry.rootBounds?.height ?? 0) / scaleFactor;

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
    entry.startConvergenceObserving?.();
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
