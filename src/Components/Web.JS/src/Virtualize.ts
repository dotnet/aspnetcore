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
  alignToItem,
  beginProgrammaticScroll,
  isFollowingBottom,
};

const dispatcherObserversByDotNetIdPropname = Symbol();
const THROTTLE_MS = 50;
const SpacerVisibilityReason = {
  UserScroll: 0,
  ProgrammaticScroll: 1,
  ViewportFill: 2,
  RenderedContentMeasurement: 3,
} as const;

const ViewportFillDirection = {
  Covered: 0,
  Before: 1,
  After: 2,
} as const;

const ScrollSource = {
  None: 0,
  UserScroll: 1,
  AlignToItem: 2,
  RestoreSnapshot: 3,
} as const;
type ScrollSource = typeof ScrollSource[keyof typeof ScrollSource];

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

  // Applies one-time base style (flex-shrink) on first sight of an element.
  const baseStylesAppliedProp = Symbol();
  function ensureBaseStyles(el: HTMLElement): void {
    if ((el as any)[baseStylesAppliedProp]) {
      return;
    }
    (el as any)[baseStylesAppliedProp] = true;
    el.style.flexShrink = '0';
  }

  const layoutAttrs = [
    ['data-blazor-virtualize-reserved-height', 'height', (n: number) => `${n}px`],
    ['data-blazor-virtualize-loop-breaker-transform', 'transform', (n: number) => `translateY(${n}px)`],
  ] as const;
  const layoutAttrNames = layoutAttrs.map(([a]) => a);
  function applyLayoutAttrs(el: HTMLElement): void {
    ensureBaseStyles(el);
    for (const [attr, styleProp, format] of layoutAttrs) {
      const raw = el.getAttribute(attr);
      const n = raw ? Number(raw) : NaN;
      if (Number.isFinite(n)) {
        el.style.setProperty(styleProp, format(n));
      } else {
        el.style.removeProperty(styleProp);
      }
    }
  }

  // Apply layout attributes before the MutationObserver starts catching changes.
  function applyLayoutAttrsBetweenSpacers(): void {
    for (let el: Element | null = spacerBefore;
         el && el !== spacerAfter.nextElementSibling;
         el = el.nextElementSibling) {
      if (layoutAttrNames.some(a => el!.hasAttribute(a))) {
        applyLayoutAttrs(el as HTMLElement);
      }
    }
  }
  applyLayoutAttrsBetweenSpacers();

  if (useNativeAnchoring) {
    // Prevent spacers from being used as scroll anchors — only rendered items should anchor.
    spacerBefore.style.overflowAnchor = 'none';
    spacerAfter.style.overflowAnchor = 'none';
  } else {
    // Manual compensation path for tables and browsers without native anchoring.
    scrollElement.style.overflowAnchor = 'none';
  }

  // Observe only the two spacers we already hold references to. Placeholders are siblings between them,
  // so on each spacer mutation we walk the sibling chain to reapply styles.
  const mutationObserver = new MutationObserver(applyLayoutAttrsBetweenSpacers);

  function flushPendingStyleMutations(): void {
    if (mutationObserver.takeRecords().length > 0) {
      applyLayoutAttrsBetweenSpacers();
    }
  }
  const spacerObserverOptions: MutationObserverInit = {
    attributes: true,
    attributeFilter: layoutAttrNames,
  };
  mutationObserver.observe(spacerBefore, spacerObserverOptions);
  mutationObserver.observe(spacerAfter, spacerObserverOptions);

  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: scrollContainer,
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(spacerBefore);
  intersectionObserver.observe(spacerAfter);

  const convergence = {
    top: false,
    bottom: false,
    items: new Set<Element>(),
    isConverging(): boolean {
      return this.top || this.bottom;
    },
  };

  const nativeAnchoring = {
    suspendedFor: new Set<'convergence' | 'slide'>(),
    suspend(reason: 'convergence' | 'slide'): void {
      this.suspendedFor.add(reason);
      if (useNativeAnchoring) {
        scrollElement.style.overflowAnchor = 'none';
      }
    },
    resume(reason: 'convergence' | 'slide'): void {
      if (!this.suspendedFor.delete(reason)) {
        return;
      }
      if (useNativeAnchoring && this.suspendedFor.size === 0) {
        scrollElement.style.overflowAnchor = '';
      }
    },
  };

  const anchoredItems: Map<Element, number> = new Map();
  let scrollTriggeredRender = false;

  const scrollActivity = {
    source: ScrollSource.None as ScrollSource,
    _ignoreNextScroll: false,
    ignoreNextScroll(): void {
      this._ignoreNextScroll = true;
    },
    consumeIgnoreScroll(): boolean {
      if (!this._ignoreNextScroll) {
        return false;
      }
      this._ignoreNextScroll = false;
      return true;
    },
    consumeScroll(): void {
      this.source = ScrollSource.None;
    },
    clear(): void {
      this.consumeScroll();
      reobserveSpacers();
    },
  };
  const isViewportAtBottom = (): boolean =>
    scrollElement.scrollHeight <= scrollElement.clientHeight
    || Math.abs(scrollElement.scrollTop + scrollElement.clientHeight - scrollElement.scrollHeight) < 2;
  const bottomTracking = {
    // Was the viewport at the bottom as of the last render? Drives the append re-pin.
    wasAtBottomLastRender: false,
    // Has the viewport actually reached the bottom? Not set at mount, stays sticky across appends.
    reached: false,
    // Follow intent: true in End mode (or after a user-initiated End-key jump) until the user scrolls away. Drives the C# scroll-to-bottom path in End mode.
    following: (anchorMode & 2) !== 0,
  };
  const clearBottomFollow = () => {
    bottomTracking.following = false;
    bottomTracking.reached = false;
    bottomTracking.wasAtBottomLastRender = false;
  };
  const anchorModeIs = {
    get none(): boolean { return anchorMode === 0; },
    get beginning(): boolean { return (anchorMode & 1) !== 0; },
    get end(): boolean { return (anchorMode & 2) !== 0; },
  };
  const isAtScrollTop = (): boolean => scrollElement.scrollTop < 1;
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

  // Called by C# at the start of a programmatic ScrollToItem, before the align scroll itself.
  function beginProgrammaticScroll(): void {
    stopConvergenceObserving();
    clearBottomFollow();
    scrollActivity.source = ScrollSource.AlignToItem;
    pendingCallbacks.delete(spacerBefore);
    pendingCallbacks.delete(spacerAfter);
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
    if (convergence.isConverging()) {
      scrollElement.scrollTop = convergence.bottom ? scrollElement.scrollHeight : 0;
      const spacer = convergence.bottom ? spacerAfter : spacerBefore;
      if (spacer.offsetHeight === 0) {
        stopConvergenceObserving();
      }
    }

    let spacerResized = false;
    for (const entry of entries) {
      if (entry.target === spacerBefore || entry.target === spacerAfter) {
        spacerResized = true;
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
      return;
    }

    if (spacerResized) {
      nativeAnchoring.resume('slide');
    }
  });

  // Always observe both spacers for the IntersectionObserver re-trigger.
  resizeObserver.observe(spacerBefore);
  resizeObserver.observe(spacerAfter);

  function refreshObservedElements(isLoading: boolean): void {
    // Ensure spacers are always observed (idempotent).
    resizeObserver.observe(spacerBefore);
    resizeObserver.observe(spacerAfter);

    // During convergence, keep the observed element set in sync with the DOM
    // and force scroll position to prevent bounce-back between renders.
    if (convergence.isConverging()) {
      if (convergence.bottom) {
        scrollElement.scrollTop = scrollElement.scrollHeight;
      } else if (convergence.top) {
        scrollElement.scrollTop = 0;
      }

      const currentItems: Set<Element> = new Set();
      for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
        resizeObserver.observe(el);
        currentItems.add(el);
      }
      // Unobserve items removed during re-render.
      for (const el of convergence.items) {
        if (!currentItems.has(el)) {
          resizeObserver.unobserve(el);
        }
      }
      convergence.items = currentItems;
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
    const wasScrollTriggered = scrollTriggeredRender;
    scrollTriggeredRender = false;

    if (!wasScrollTriggered || isLoading) {
      nativeAnchoring.resume('slide');
    }

    // End mode: pin new items into view if we're at the bottom now, or were and are still following.
    if ((anchorModeIs.end || bottomTracking.following) && (bottomTracking.wasAtBottomLastRender || bottomTracking.reached)) {
      scrollElement.scrollTop = scrollElement.scrollHeight;
      scrollActivity.ignoreNextScroll();
      // Start convergence only when there are more items to load (spacerAfter > 0).
      // When all items fit in DOM, the single scrollTop assignment above is sufficient.
      if (!convergence.bottom && !convergence.top && spacerAfter.offsetHeight > 0) {
        scrollActivity.clear();
        startConvergenceObserving('bottom');
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
          scrollActivity.ignoreNextScroll();
        }
      }
    }

    // Capture the first visible item's position after each render.
    updateAnchorSnapshot();

  }

  // Corrects scrollTop after a render that shifted content, using the snapshot
  // saved by updateAnchorSnapshot() during the previous render cycle.
  function restoreAnchorForShift(): void {
    // Apply styles before we read layout
    flushPendingStyleMutations();

    const snapshot = observersByDotNetObjectId[id].anchorSnapshot;
    if (!snapshot) {
      return;
    }
    observersByDotNetObjectId[id].anchorSnapshot = null;

    if (convergence.isConverging()) {
      return;
    }

    // Retry a pending programmatic alignment now that items may be in DOM.
    if (pendingAlignLocalIndex !== null) {
      const pending = pendingAlignLocalIndex;
      pendingAlignLocalIndex = null;
      alignToItemAt(pending);
      return;
    }

    // Beginning mode at the very top: show new items by converging to top.
    if (anchorModeIs.beginning && snapshot.scrollTop < 1) {
      scrollElement.scrollTop = 0;
      startConvergenceObserving('top');
      reobserveSpacers();
      return;
    }

    const newOffset = measureLocalChildOffset(snapshot.anchorItemIndex);
    if (Number.isNaN(newOffset)) {
      return;
    }
    const delta = newOffset - snapshot.anchorOffset;

    // Save anchor for drift correction.
    scrollActivity.source = ScrollSource.RestoreSnapshot;
    scrollActivity.ignoreNextScroll();
    if (Math.abs(delta) > 1) {
      scrollCorrectionItemIndex = snapshot.anchorItemIndex;
      pendingScrollCorrection = true;
    }

    // End mode: only carry the at-bottom state forward if the viewport is actually at the bottom right now.
    // Don't rely on the cached wasAtBottomLastRender — it may be stale if the user scrolled away.
    const preserveWasAtBottom = anchorModeIs.end && isViewportAtBottom();

    if (Math.abs(delta) > 1) {
      scrollElement.scrollTop += delta;
    }

    // Save anchor offset AFTER scrollTop adjustment for drift correction.
    if (pendingScrollCorrection) {
      const correctedOffset = measureLocalChildOffset(snapshot.anchorItemIndex);
      if (!Number.isNaN(correctedOffset)) {
        scrollCorrectionOffset = correctedOffset;
      }
    }

    if (preserveWasAtBottom) {
      bottomTracking.wasAtBottomLastRender = true;
    }
  }

  function startConvergenceObserving(direction: 'top' | 'bottom'): void {
    const alreadyConverging = convergence.isConverging();
    convergence[direction] = true;
    if (alreadyConverging) return;
    nativeAnchoring.suspend('convergence');
    for (let el = spacerBefore.nextElementSibling; el && el !== spacerAfter; el = el.nextElementSibling) {
      resizeObserver.observe(el);
      convergence.items.add(el);
    }
  }

  function stopConvergenceObserving(): void {
    if (!convergence.isConverging()) return;
    convergence.top = false;
    convergence.bottom = false;
    for (const el of convergence.items) {
      resizeObserver.unobserve(el);
    }
    convergence.items.clear();
    nativeAnchoring.resume('convergence');
    anchoredItems.clear();
    // Take a fresh snapshot so the next anchor restore has valid data.
    updateAnchorSnapshot();
  }

  let pendingJumpToEnd = false;
  let pendingJumpToStart = false;

  function handleUserScrollInput(): void {
    const selfScrollInProgress = scrollActivity.source === ScrollSource.AlignToItem
      || scrollActivity.source === ScrollSource.RestoreSnapshot;
    scrollActivity.consumeIgnoreScroll();
    scrollActivity.source = ScrollSource.UserScroll;
    if (selfScrollInProgress) {
      reobserveSpacers();
    }
  }

  function handleUserPointerMove(e: Event): void {
    if ((e as PointerEvent).buttons !== 0) {
      handleUserScrollInput();
    }
  }

  function isUserScrollKey(key: string): boolean {
    return ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', ' '].includes(key);
  }

  const keydownTarget: EventTarget = scrollContainer || document;
  function handleJumpKeys(e: Event): void {
    const ke = e as KeyboardEvent;
    if (ke.key === 'End') {
      scrollActivity.source = ScrollSource.UserScroll;
      reobserveSpacers();
      pendingJumpToEnd = true;
      pendingJumpToStart = false;
      if (!anchorModeIs.end) {
        bottomTracking.following = true;
        bottomTracking.reached = true;
      }
      if (!convergence.bottom && spacerAfter.offsetHeight > 0) {
        startConvergenceObserving('bottom');
      }
    } else if (ke.key === 'Home') {
      scrollActivity.source = ScrollSource.UserScroll;
      reobserveSpacers();
      pendingJumpToStart = true;
      pendingJumpToEnd = false;
      clearBottomFollow();
      if (!convergence.top && spacerBefore.offsetHeight > 0) {
        startConvergenceObserving('top');
      }
    } else if (isUserScrollKey(ke.key)) {
      handleUserScrollInput();
    }
  }

  const scrollEventTarget: EventTarget = scrollContainer ?? window;
  function subscribeToUserScroll(): void {
    keydownTarget.addEventListener('keydown', handleJumpKeys);
    scrollEventTarget.addEventListener('wheel', handleUserScrollInput, { passive: true });
    scrollEventTarget.addEventListener('touchmove', handleUserScrollInput, { passive: true });
    scrollEventTarget.addEventListener('pointermove', handleUserPointerMove, { passive: true });
  }

  function unsubscribeFromUserScroll(): void {
    keydownTarget.removeEventListener('keydown', handleJumpKeys);
    scrollEventTarget.removeEventListener('wheel', handleUserScrollInput);
    scrollEventTarget.removeEventListener('touchmove', handleUserScrollInput);
    scrollEventTarget.removeEventListener('pointermove', handleUserPointerMove);
  }

  function subscribeToScroll(): void {
    scrollEventTarget.addEventListener('scroll', handleScroll, { passive: true });
  }

  function unsubscribeFromScroll(): void {
    scrollEventTarget.removeEventListener('scroll', handleScroll);
  }

  function handleScroll(): void {
    if (convergence.isConverging() || scrollActivity.consumeIgnoreScroll()) {
      return;
    }

    const selfScrollInProgress = scrollActivity.source === ScrollSource.AlignToItem
      || scrollActivity.source === ScrollSource.RestoreSnapshot;
    if (selfScrollInProgress) {
      return;
    }
    scrollActivity.source = ScrollSource.UserScroll;

    // A user scroll is the only thing that (re)sets follow state (self-scrolls early-return above).
    if (anchorModeIs.end || bottomTracking.following) {
      const atBottom = isViewportAtBottom();
      bottomTracking.following = atBottom;
      bottomTracking.reached = atBottom;
    }

    updateAnchorSnapshot();
  }

  subscribeToUserScroll();
  subscribeToScroll();

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  let pendingCallbacks: Map<Element, IntersectionObserverEntry> = new Map();
  let callbackTimeout: ReturnType<typeof setTimeout> | null = null;
  let pendingAlignLocalIndex: number | null = null;

  // Walks `localIndex` siblings forward from spacerBefore to find the rendered child,
  // returning its viewport-relative top measured against the scroll container (or 0 for
  // the window-scroll case). Returns NaN when the slot is missing — e.g., the row hasn't
  // rendered yet, or the local index falls outside the currently rendered window.
  function measureLocalChildOffset(localIndex: number): number {
    let el: Element | null = spacerBefore.nextElementSibling;
    for (let i = 0; i < localIndex && el && el !== spacerAfter; i++) {
      el = el.nextElementSibling;
    }
    if (!el || el === spacerAfter) {
      return Number.NaN;
    }
    const containerTop = scrollElement === document.documentElement
      ? 0
      : scrollElement.getBoundingClientRect().top;
    return el.getBoundingClientRect().top - containerTop;
  }

  function reportRenderedContentMeasurement(): void {
    const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);
    rangeBetweenSpacers.setStartAfter(spacerBefore);
    rangeBetweenSpacers.setEndBefore(spacerAfter);
    const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height / scaleFactor;
    const containerSize = scrollElement.getBoundingClientRect().height / scaleFactor;
    dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', 0, spacerSeparation, containerSize, SpacerVisibilityReason.RenderedContentMeasurement);
  }

  // Measures the target's viewport-relative top and aligns it to containerTop.
  function alignToItemAt(localIndex: number): number | null {
    function beginAlign(): void {
      scrollActivity.ignoreNextScroll();
      scrollActivity.source = ScrollSource.AlignToItem;
      observersByDotNetObjectId[id].anchorSnapshot = null;
      stopConvergenceObserving();
    }

    // Target row should be measured against the committed window, not a stale spacer height.
    flushPendingStyleMutations();
    const alignmentOffset = isTable
      && scrollContainer
      && (localIndex !== 0 || spacerBefore.offsetHeight !== 0)
      ? Math.min(scrollContainer.clientTop * getScaleFactor(spacerBefore, spacerAfter), 1)
      : 0;
    const delta = measureLocalChildOffset(localIndex) + alignmentOffset;
    if (Number.isNaN(delta)) {
      // Target item isn't in the committed window.
      pendingAlignLocalIndex = localIndex;
      beginAlign();
      return null;
    }
    pendingAlignLocalIndex = null;

    reportRenderedContentMeasurement();

    if (Math.abs(delta) > 0.5) {
      beginAlign();
      pendingJumpToStart = false;
      pendingJumpToEnd = false;
      scrollElement.scrollTo({ top: scrollElement.scrollTop + delta, behavior: 'instant' });
    }

    return getViewportFillDirection();
  }

  function getViewportBounds(scaleFactor: number): { top: number; bottom: number } {
    let viewportTop = 0;
    let viewportBottom = document.documentElement.clientHeight;
    if (scrollContainer) {
      const scrollContainerRect = scrollContainer.getBoundingClientRect();
      viewportTop = scrollContainerRect.top + scrollContainer.clientTop * scaleFactor;
      viewportBottom = viewportTop + scrollContainer.clientHeight * scaleFactor;
    }
    return { top: viewportTop, bottom: viewportBottom };
  }

  function occupiesViewport(spacer: HTMLElement, viewport: { top: number; bottom: number }): boolean {
    const spacerRect = spacer.getBoundingClientRect();
    return Math.min(spacerRect.bottom, viewport.bottom) > Math.max(spacerRect.top, viewport.top);
  }

  function getViewportFillDirection(): number {
    const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);
    const viewport = getViewportBounds(scaleFactor);
    if (occupiesViewport(spacerBefore, viewport)) {
      return ViewportFillDirection.Before;
    }
    if (occupiesViewport(spacerAfter, viewport)) {
      return ViewportFillDirection.After;
    }
    return ViewportFillDirection.Covered;
  }

  observersByDotNetObjectId[id] = {
    intersectionObserver,
    resizeObserver,
    refreshObservedElements,
    scrollElement,
    startConvergenceObserving,
    isFollowingBottom: () => bottomTracking.following,
    setAnchorMode: (mode: number) => { anchorMode = mode; bottomTracking.following = (mode & 2) !== 0; bottomTracking.reached = isViewportAtBottom(); },
    restoreAnchor: restoreAnchorForShift,
    alignToItem: alignToItemAt,
    beginProgrammaticScroll: beginProgrammaticScroll,
    anchorSnapshot: null as { anchorItemIndex: number; anchorOffset: number; scrollTop: number } | null,
    onDispose: () => {
      mutationObserver.disconnect();
      stopConvergenceObserving();
      anchoredItems.clear();
      resizeObserver.disconnect();
      unsubscribeFromUserScroll();
      unsubscribeFromScroll();
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

  function updateBottomConvergence(userScrolled: boolean): void {
    if (spacerAfter.offsetHeight === 0) {
      if (convergence.bottom) {
        stopConvergenceObserving();
      }
      return;
    }
    if (convergence.bottom) return;

    // pendingJumpToEnd is user-initiated (End key) — always honor it.
    // Data-driven convergence only fires when End anchoring is enabled.
    if (pendingJumpToEnd) {
      startConvergenceObserving('bottom');
      scrollElement.scrollTop = scrollElement.scrollHeight;
      pendingJumpToEnd = false;
      return;
    }

    if (!anchorModeIs.end && !userScrolled) return;

    const atBottom = scrollElement.scrollTop + scrollElement.clientHeight >= scrollElement.scrollHeight - 1;
    if (!atBottom) return;

    startConvergenceObserving('bottom');
  }

  function updateTopConvergence(): void {
    if (spacerBefore.offsetHeight === 0) {
      if (convergence.top) {
        stopConvergenceObserving();
      }
      return;
    }
    if (convergence.top) return;

    // pendingJumpToStart is user-initiated (Home key) — always honor it.
    // Data-driven convergence only fires when Beginning anchoring is enabled.
    if (pendingJumpToStart) {
      startConvergenceObserving('top');
      scrollElement.scrollTop = 0;
      pendingJumpToStart = false;
      return;
    }

    if (!anchorModeIs.beginning) return;

    const atTop = isAtScrollTop();
    if (!atTop) return;

    startConvergenceObserving('top');
  }

  // Saves the first visible item's child index and viewport-relative position.
  function updateAnchorSnapshot(): void {
    bottomTracking.wasAtBottomLastRender = isViewportAtBottom();

    const containerTop = scrollContainer
      ? scrollContainer.getBoundingClientRect().top
      : 0;

    let anchorItemIndex = 0;
    for (let el = spacerBefore.nextElementSibling;
      el && el !== spacerAfter;
      el = el.nextElementSibling) {
      const rect = el.getBoundingClientRect();
      if (rect.bottom > containerTop) {
        const existing = observersByDotNetObjectId[id].anchorSnapshot;
        const nativeAnchoringUnavailable = !useNativeAnchoring || isAtScrollTop();
        // Keep the pre-shift snapshot for None/End modes, and for Start modes that are not actively
        // converging to the top (during top convergence the viewport is repositioned instead).
        const modePinsTopItem = !anchorModeIs.beginning || !convergence.top;
        const itemAlreadyShifted = rect.top - containerTop > rect.height;
        if (nativeAnchoringUnavailable && modePinsTopItem && existing && itemAlreadyShifted) {
          return;
        }
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

    const source = scrollActivity.source;
    if (source === ScrollSource.UserScroll) {
      // An ongoing scroll re-arms UserScroll every tick (handleUserScroll). Consuming prevents stale activity status.
      scrollActivity.consumeScroll();
    }

    // Keep the anchor snapshot fresh on every IO callback so it reflects the current scroll position,
    // not just the last render. Skip while a self-scroll is settling — those callbacks have stale data.
    const isSelfScroll = source === ScrollSource.AlignToItem || source === ScrollSource.RestoreSnapshot;
    if (!isSelfScroll) {
      updateAnchorSnapshot();
    }

    const bothSpacersIntersect = entries.some(entry => entry.target === spacerBefore && entry.isIntersecting)
      && entries.some(entry => entry.target === spacerAfter && entry.isIntersecting);

    const intersectingEntries = entries.filter(entry => {
      if (bothSpacersIntersect && entry.target === spacerAfter) {
        // When both spacers are visible, report only the before spacer to avoid conflicting callbacks.
        return false;
      }

      if (entry.isIntersecting) {
        if (!isSelfScroll) {
          // Convergence to the top/bottom edge should not fight with self scroll.
          if (entry.target === spacerAfter) {
            updateBottomConvergence(source === ScrollSource.UserScroll);
          } else if (entry.target === spacerBefore) {
            updateTopConvergence();
          }
        }
        return true;
      }
      if (entry.target === spacerAfter && convergence.bottom && spacerAfter.offsetHeight > 0) {
        scrollElement.scrollTop = scrollElement.scrollHeight;
      } else if (entry.target === spacerBefore && convergence.top && spacerBefore.offsetHeight > 0) {
        scrollElement.scrollTop = 0;
      }
      return false;
    });

    if (intersectingEntries.length === 0) {
      if (source === ScrollSource.AlignToItem) {
        scrollActivity.clear();
      }
      return;
    }

    const scaleFactor = getScaleFactor(spacerBefore, spacerAfter);

    rangeBetweenSpacers.setStartAfter(spacerBefore);
    rangeBetweenSpacers.setEndBefore(spacerAfter);
    const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height / scaleFactor;

    intersectingEntries.forEach((entry): void => {
      const containerSize = (entry.rootBounds?.height ?? 0) / scaleFactor;
      const reason = source === ScrollSource.UserScroll
        ? SpacerVisibilityReason.UserScroll
        : (isSelfScroll && (entry.target === spacerBefore || source === ScrollSource.RestoreSnapshot))
          ? SpacerVisibilityReason.ProgrammaticScroll
          : SpacerVisibilityReason.ViewportFill;

      const isBefore = entry.target === spacerBefore;
      const spacer = isBefore ? spacerBefore : spacerAfter;

      if (!isBefore && spacer.offsetHeight === 0) {
        return;
      }

      // ProgrammaticScroll callbacks are ignored by C#, so no item redistribution and no re-render happens.
      if (reason !== SpacerVisibilityReason.ProgrammaticScroll) {
        // So that RefreshObservedElements can skip item observation (avoids layout interference drift).
        scrollTriggeredRender = true;
        if (spacer.offsetHeight > 0) {
          nativeAnchoring.suspend('slide');
        }
      }

      const spacerSize = isBefore
        ? (entry.intersectionRect.top - entry.boundingClientRect.top) / scaleFactor
        : (entry.boundingClientRect.bottom - entry.intersectionRect.bottom) / scaleFactor;
      const methodName = isBefore ? 'OnSpacerBeforeVisible' : 'OnSpacerAfterVisible';
      dotNetHelper.invokeMethodAsync(methodName, spacerSize, spacerSeparation, containerSize, reason);
    });

    if (source === ScrollSource.AlignToItem) {
      scrollActivity.clear();
    }
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
  if (entry && entry.isFollowingBottom?.()) {
    entry.scrollElement.scrollTop = entry.scrollElement.scrollHeight;
    entry.startConvergenceObserving?.('bottom');
  }
}

function refreshObservers(dotNetHelper: DotNet.DotNetObject, isLoading: boolean): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  const entry = observersByDotNetObjectId[id];
  entry?.refreshObservedElements?.(isLoading);
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

function alignToItem(dotNetHelper: DotNet.DotNetObject, localIndex: number): number | null {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  return observersByDotNetObjectId[id]?.alignToItem?.(localIndex) ?? null;
}

function beginProgrammaticScroll(dotNetHelper: DotNet.DotNetObject): void {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  observersByDotNetObjectId[id]?.beginProgrammaticScroll?.();
}

function isFollowingBottom(dotNetHelper: DotNet.DotNetObject): boolean {
  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  return observersByDotNetObjectId[id]?.isFollowingBottom?.() ?? false;
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
