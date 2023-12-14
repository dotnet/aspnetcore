// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export const Virtualize = {
  init,
  dispose,
};

const dispatcherObserversByDotNetIdPropname = Symbol();

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

  const { observersByDotNetObjectId, id } = getObserversMapEntry(dotNetHelper);
  observersByDotNetObjectId[id] = {
    intersectionObserver,
    mutationObserverBefore,
    mutationObserverAfter,
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

  function intersectionCallback(entries: IntersectionObserverEntry[]): void {
    entries.forEach((entry): void => {
      if (!entry.isIntersecting) {
        return;
      }

      // To compute the ItemSize, work out the separation between the two spacers. We can't just measure an individual element
      // because each conceptual item could be made from multiple elements. Using getBoundingClientRect allows for the size to be
      // a fractional value. It's important not to add or subtract any such fractional values (e.g., to subtract the 'top' of
      // one item from the 'bottom' of another to get the distance between them) because floating point errors would cause
      // scrolling glitches.
      rangeBetweenSpacers.setStartAfter(spacerBefore);
      rangeBetweenSpacers.setEndBefore(spacerAfter);
      const spacerSeparation = rangeBetweenSpacers.getBoundingClientRect().height;
      const containerSize = entry.rootBounds?.height;

      if (entry.target === spacerBefore) {
        dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', entry.intersectionRect.top - entry.boundingClientRect.top, spacerSeparation, containerSize);
      } else if (entry.target === spacerAfter && spacerAfter.offsetHeight > 0) {
        // When we first start up, both the "before" and "after" spacers will be visible, but it's only relevant to raise a
        // single event to load the initial data. To avoid raising two events, skip the one for the "after" spacer if we know
        // it's meaningless to talk about any overlap into it.
        dotNetHelper.invokeMethodAsync('OnSpacerAfterVisible', entry.boundingClientRect.bottom - entry.intersectionRect.bottom, spacerSeparation, containerSize);
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

    dotNetHelper.dispose();

    delete observersByDotNetObjectId[id];
  }
}
