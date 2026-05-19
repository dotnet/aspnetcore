// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export const Virtualize = {
  init,
  dispose,
  scrollToBottom,
  refreshObservers,
  setAnchorMode,
  restoreAnchor,
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

function init(dotNetHelper: DotNet.DotNetObject, spacerBefore: HTMLElement, spacerAfter: HTMLElement, anchorMode = 1, rootMargin = 50): void {
  // If the component was disposed before the JS interop call completed, the element references may be null
  // or the elements may have been disconnected from the DOM. Return early to avoid errors.
  if (!spacerBefore || !spacerAfter || !spacerBefore.isConnected || !spacerAfter.isConnected) {
    return;
  }

  const scrollContainer = findClosestScrollContainer(spacerBefore);
  const scrollElement = scrollContainer || document.documentElement;
  const isTable = isValidTableElement(spacerAfter.parentElement);

  // Ensure the scroll container is focusable for Home/End key handling.
  // Use tabindex="-1" so it's focusable via click/JS but not added to the tab order.
  if (scrollContainer && !scrollContainer.hasAttribute('tabindex')) {
    scrollContainer.setAttribute('tabindex', '-1');
  }
  const supportsAnchor = CSS.supports('overflow-anchor', 'auto');
  const useNativeAnchoring = !isTable && supportsAnchor;

  const rangeBetweenSpacers = document.createRange();

  if (isTable) {
    spacerBefore.style.display = 'table-row';
    spacerAfter.style.display = 'table-row';
  }

  if (useNativeAnchoring) {
    // Prevent spacers from being used as scroll anchors — only rendered items should anchor.
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

  let convergingElements = false;
  let convergenceItems: Set<Element> = new Set();

  const anchoredItems: Map<Element, number> = new Map();
  let scrollTriggeredRender = false;

  // After anchor restore, suppress spacer IO callbacks until the next user scroll.
  let suppressSpacerCallbacks = false;
  let ignoreAnchorScroll = false;
  // Whether the viewport was at the bottom before the last render (for End-mode follow).
  let wasAtBottom = false;
  // Pending scroll correction after redistribution changes spacer→item heights.
  let pendingScrollCorrection = false;
  let scrollCorrectionItemIndex = 0;
  let scrollCorrectionOffset = 0;

  function reobserveSpacers(): void {
    intersectionObserver.unobserve(spacerBefore);
    intersectionObserver.observe(spacerBefore);
    intersectionObserver.unobserve(spacerAfter);
    intersectionObserver.observe(spacerAfter);
  }

  function getObservedHeight(entry: ResizeObserverEntry): number {
    return entry.borderBoxSize?.[0]?.blockSize ?? entry.contentRect.height;
  }

  function compensateScrollForItemResizes(entries: ResizeObserverEntry[]): void {
    let scrollDelta = 0;
    const containerTop = scrollContainer
      ? scrollContainer.getBoundingClientRect().top
      : 0;

    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        continue;
      }

      if (entry.target.isConnected) {
        const el = entry.target as HTMLElement;
        const oldHeight = anchoredItems.get(el);
        const newHeight = getObservedHeight(entry);
        anchoredItems.set(el, newHeight);

        if (oldHeight !== undefined && oldHeight !== newHeight) {
          if (el.getBoundingClientRect().top < containerTop) {
            scrollDelta += (newHeight - oldHeight);
          }
        }
      }
    }

    if (scrollDelta !== 0 && scrollElement.scrollTop > 0) {
      scrollElement.scrollTop += scrollDelta;
    }
  }

  // ResizeObserver roles:
  //  1. Always observes both spacers so that when a spacer resizes we re-trigger the
  //     IntersectionObserver — which otherwise won't fire again for an element that is already visible.
  //  2. For convergence (sticky-top/bottom) - observes elements for geometry changes, drives the scroll position.
  //  3. Manual scroll compensation (tables/Safari) — adjusts scrollTop when above-viewport items resize.
  const resizeObserver = new ResizeObserver((entries: ResizeObserverEntry[]): void => {
    // Convergence logic: keep scroll pinned to top/bottom while items load.
    // Do this before re-observing spacers so the IO callback sees the correct
    // scroll position, not the stale one from before the spacer resize.
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

    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        const spacer = entry.target as HTMLElement;
        if (spacer.isConnected) {
          intersectionObserver.unobserve(spacer);
          intersectionObserver.observe(spacer);
        }
      }
    }

    // Manual scroll compensation: adjust scrollTop for above-viewport resizes.
    if (!useNativeAnchoring) {
      compensateScrollForItemResizes(entries);
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
      spacerBefore.style.overflowAnchor = 'none';
      spacerAfter.style.overflowAnchor = 'none';
    }

    // Ensure spacers are always observed (idempotent).
    resizeObserver.observe(spacerBefore);
    resizeObserver.observe(spacerAfter);

    // During convergence, keep the observed element set in sync with the DOM
    // and force scroll position to prevent bounce-back between renders.
    if (convergingElements) {
      if (convergingToBottom) {
        scrollElement.scrollTop = scrollElement.scrollHeight;
      } else if (convergingToTop) {
        scrollElement.scrollTop = 0;
      }

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
      return;
    }

    // Manual compensation: observe items so ResizeObserver can compensate scrollTop.
    // Skip for native anchoring (browser handles it) and scroll-triggered renders
    // (avoids layout interference drift).
    if (!useNativeAnchoring && !scrollTriggeredRender) {
      const currentItems = new Set<Element>();
      for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
        resizeObserver.observe(el);
        currentItems.add(el);
      }

      for (const [el] of anchoredItems) {
        if (!currentItems.has(el)) {
          resizeObserver.unobserve(el);
          anchoredItems.delete(el);
        }
      }
    }
    scrollTriggeredRender = false;

    // End mode: scroll to bottom when items were appended while viewport was at bottom.
    if ((anchorMode & 2) && wasAtBottom) {
      scrollElement.scrollTop = scrollElement.scrollHeight;
      ignoreAnchorScroll = true;
      // Start convergence only when there are more items to load (spacerAfter > 0).
      // When all items fit in DOM, the single scrollTop assignment above is sufficient.
      if (!convergingToBottom && !convergingToTop && spacerAfter.offsetHeight > 0) {
        convergingToBottom = true;
        suppressSpacerCallbacks = false;
        reobserveSpacers();
        startConvergenceObserving();
      }
    }

    // Correct drift from spacer→item height differences after redistribution.
    if (pendingScrollCorrection) {
      let el: Element | null = spacerBefore.nextElementSibling;
      for (let i = 0; i < scrollCorrectionItemIndex && el && el !== spacerAfter; i++) {
        el = el.nextElementSibling;
      }
      if (el && el !== spacerAfter) {
        pendingScrollCorrection = false;
        const containerTop = scrollContainer ? scrollContainer.getBoundingClientRect().top : 0;
        const delta = (el.getBoundingClientRect().top - containerTop) - scrollCorrectionOffset;
        if (Math.abs(delta) > 1) {
          scrollElement.scrollTop += delta;
          ignoreAnchorScroll = true;
        }
      }
    }

    // Capture the first visible item's position after each render.
    updateAnchorSnapshot();

  }

  // Corrects scrollTop after a render that shifted content, using the snapshot
  // saved by updateAnchorSnapshot() during the previous render cycle.
  function restoreAnchorForShift(): void {
    const snapshot = observersByDotNetObjectId[id].anchorSnapshot;
    if (!snapshot) {
      return;
    }
    observersByDotNetObjectId[id].anchorSnapshot = null;

    if (convergingToTop || convergingToBottom) {
      return;
    }

    // Beginning mode at the very top: show new items by converging to top.
    if ((anchorMode & 1) && snapshot.scrollTop < 1) {
      convergingToTop = true;
      scrollElement.scrollTop = 0;
      startConvergenceObserving();
      return;
    }

    let current = spacerBefore.nextElementSibling;
    for (let i = 0; i < snapshot.anchorItemIndex && current && current !== spacerAfter; i++) {
      current = current.nextElementSibling;
    }

    if (!current || current === spacerAfter) {
      return;
    }

    const containerTop = scrollContainer
      ? scrollContainer.getBoundingClientRect().top
      : 0;
    const newOffset = current.getBoundingClientRect().top - containerTop;
    const delta = newOffset - snapshot.anchorOffset;

    // Suppress spacer IO until next user scroll. Save anchor for drift correction.
    suppressSpacerCallbacks = true;
    ignoreAnchorScroll = true;
    if (Math.abs(delta) > 1) {
      scrollCorrectionItemIndex = snapshot.anchorItemIndex;
      pendingScrollCorrection = true;
    }

    // End mode: preserve wasAtBottom only if the viewport is actually at the bottom right now.
    // Don't rely on the cached wasAtBottom — it may be stale if the user scrolled away.
    const atBottom = scrollElement.scrollHeight <= scrollElement.clientHeight
      || Math.abs(scrollElement.scrollTop + scrollElement.clientHeight - scrollElement.scrollHeight) < 2;
    const preserveWasAtBottom = (anchorMode & 2) && atBottom;

    if (Math.abs(delta) > 1) {
      scrollElement.scrollTop += delta;
    }

    // Save anchor offset AFTER scrollTop adjustment for drift correction.
    if (pendingScrollCorrection) {
      const containerTop = scrollContainer ? scrollContainer.getBoundingClientRect().top : 0;
      scrollCorrectionOffset = current.getBoundingClientRect().top - containerTop;
    }

    if (preserveWasAtBottom) {
      wasAtBottom = true;
    }
  }

  function startConvergenceObserving(): void {
    if (convergingElements) return;
    convergingElements = true;
    if (useNativeAnchoring) {
      scrollElement.style.overflowAnchor = 'none';
    }
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
    if (useNativeAnchoring) {
      scrollElement.style.overflowAnchor = '';
    }
    anchoredItems.clear();
    // Take a fresh snapshot so the next anchor restore has valid data.
    updateAnchorSnapshot();
  }

  let convergingToBottom = false;
  let convergingToTop = false;

  let pendingJumpToEnd = false;
  let pendingJumpToStart = false;

  const keydownTarget: EventTarget = scrollContainer || document;
  function handleJumpKeys(e: Event): void {
    const ke = e as KeyboardEvent;
    if (ke.key === 'End') {
      suppressSpacerCallbacks = false;
      reobserveSpacers();
      pendingJumpToEnd = true;
      pendingJumpToStart = false;
      if (!convergingToBottom && spacerAfter.offsetHeight > 0) {
        convergingToBottom = true;
        startConvergenceObserving();
      }
    } else if (ke.key === 'Home') {
      suppressSpacerCallbacks = false;
      reobserveSpacers();
      pendingJumpToStart = true;
      pendingJumpToEnd = false;
      if (!convergingToTop && spacerBefore.offsetHeight > 0) {
        convergingToTop = true;
        startConvergenceObserving();
      }
    }
  }
  keydownTarget.addEventListener('keydown', handleJumpKeys);

  const scrollEventTarget: EventTarget = scrollContainer ?? window;
  function handleScroll(): void {
    if (convergingToBottom || convergingToTop) {
      return;
    }

    if (ignoreAnchorScroll) {
      ignoreAnchorScroll = false;
      return;
    }

    // Clear suppression and re-observe on user scroll.
    if (suppressSpacerCallbacks) {
      suppressSpacerCallbacks = false;
      reobserveSpacers();
    }

    updateAnchorSnapshot();
  }
  scrollEventTarget.addEventListener('scroll', handleScroll, { passive: true });

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;

  observersByDotNetObjectId[id] = {
    intersectionObserver,
    resizeObserver,
    refreshObservedElements,
    scrollElement,
    startConvergenceObserving,
    setConvergingToBottom: () => { convergingToBottom = true; },
    setAnchorMode: (mode: number) => { anchorMode = mode; },
    restoreAnchor: restoreAnchorForShift,
    anchorSnapshot: null as { anchorItemIndex: number; anchorOffset: number; scrollTop: number } | null,
    onDispose: () => {
      stopConvergenceObserving();
      anchoredItems.clear();
      resizeObserver.disconnect();
      keydownTarget.removeEventListener('keydown', handleJumpKeys);
      scrollEventTarget.removeEventListener('scroll', handleScroll);
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

    // pendingJumpToEnd is user-initiated (End key) — always honor it.
    // Data-driven convergence only fires when End anchoring is enabled.
    if (pendingJumpToEnd) {
      convergingToBottom = true;
      startConvergenceObserving();
      scrollElement.scrollTop = scrollElement.scrollHeight;
      pendingJumpToEnd = false;
      return;
    }

    if (!(anchorMode & 2)) return;

    const atBottom = scrollElement.scrollTop + scrollElement.clientHeight >= scrollElement.scrollHeight - 1;
    if (!atBottom) return;

    convergingToBottom = true;
    startConvergenceObserving();
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

    // pendingJumpToStart is user-initiated (Home key) — always honor it.
    // Data-driven convergence only fires when Beginning anchoring is enabled.
    if (pendingJumpToStart) {
      convergingToTop = true;
      startConvergenceObserving();
      scrollElement.scrollTop = 0;
      pendingJumpToStart = false;
      return;
    }

    if (!(anchorMode & 1)) return;

    const atTop = scrollElement.scrollTop < 1;
    if (!atTop) return;

    convergingToTop = true;
    startConvergenceObserving();
  }

  // Saves the first visible item's child index and viewport-relative position.
  function updateAnchorSnapshot(): void {
    wasAtBottom = scrollElement.scrollHeight <= scrollElement.clientHeight
      || Math.abs(scrollElement.scrollTop + scrollElement.clientHeight - scrollElement.scrollHeight) < 2;

    const containerTop = scrollContainer
      ? scrollContainer.getBoundingClientRect().top
      : 0;

    let anchorItemIndex = 0;
    for (let el = spacerBefore.nextElementSibling;
      el && el !== spacerAfter;
      el = el.nextElementSibling) {
      const rect = el.getBoundingClientRect();
      if (rect.bottom > containerTop) {
        observersByDotNetObjectId[id].anchorSnapshot = {
          anchorItemIndex,
          anchorOffset: rect.top - containerTop,
          scrollTop: scrollElement.scrollTop,
        };
        return;
      }
      anchorItemIndex++;
    }
    observersByDotNetObjectId[id].anchorSnapshot = null;
  }

  function processIntersectionEntries(entries: IntersectionObserverEntry[]): void {
    // Check if the spacers are still in the DOM. They may have been removed if the component was disposed.
    if (!spacerBefore.isConnected || !spacerAfter.isConnected) {
      return;
    }

    // Keep the anchor snapshot fresh on every IO callback so it reflects
    // the current scroll position, not just the last render. Skip when
    // suppression is active — those callbacks have pre-restore stale data.
    if (!suppressSpacerCallbacks) {
      updateAnchorSnapshot();
    }

    const intersectingEntries = entries.filter(entry => {
      // After an anchor restore, skip ALL spacer callbacks until the user
      // scrolls. Re-observation is handled in handleScroll.
      if (suppressSpacerCallbacks && (entry.target === spacerBefore || entry.target === spacerAfter)) {
        return false;
      }

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

      // So that RefreshObservedElements can skip item observation (avoids layout interference drift).
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
    entry.setConvergingToBottom?.();
    entry.scrollElement.scrollTop = entry.scrollElement.scrollHeight;
    entry.startConvergenceObserving?.();
  }
}

function refreshObservers(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  entry?.refreshObservedElements?.();
}

function setAnchorMode(dotNetHelper: DotNet.DotNetObject, mode: number): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  entry?.setAnchorMode?.(mode);
}

function restoreAnchor(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  entry?.restoreAnchor?.();
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


