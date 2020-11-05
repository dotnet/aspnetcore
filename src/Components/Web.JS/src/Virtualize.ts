export const Virtualize = {
  init,
  dispose,
};

const observersByDotNetId = {};

function findClosestScrollContainer(element: HTMLElement | null): HTMLElement | null {
  if (!element) {
    return null;
  }

  const style = getComputedStyle(element);

  if (style.overflowY !== 'visible') {
    return element;
  }

  return findClosestScrollContainer(element.parentElement);
}

function init(dotNetHelper: any, spacerBefore: HTMLElement, spacerAfter: HTMLElement, rootMargin = 50): void {
  // Overflow anchoring can cause an ongoing scroll loop, because when we resize the spacers, the browser
  // would update the scroll position to compensate. Then the spacer would remain visible and we'd keep on
  // trying to resize it.
  const scrollContainer = findClosestScrollContainer(spacerBefore);
  (scrollContainer || document.documentElement).style.overflowAnchor = 'none';

  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: scrollContainer,
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(spacerBefore);
  intersectionObserver.observe(spacerAfter);

  const mutationObserverBefore = createSpacerMutationObserver(spacerBefore);
  const mutationObserverAfter = createSpacerMutationObserver(spacerAfter);

  observersByDotNetId[dotNetHelper._id] = {
    intersectionObserver,
    mutationObserverBefore,
    mutationObserverAfter,
  };

  function createSpacerMutationObserver(spacer: HTMLElement): MutationObserver {
    // Without the use of thresholds, IntersectionObserver only detects binary changes in visibility,
    // so if a spacer gets resized but remains visible, no additional callbacks will occur. By unobserving
    // and reobserving spacers when they get resized, the intersection callback will re-run if they remain visible.
    const mutationObserver = new MutationObserver((): void => {
      intersectionObserver.unobserve(spacer);
      intersectionObserver.observe(spacer);
    });

    mutationObserver.observe(spacer, { attributes: true });

    return mutationObserver;
  }

  function intersectionCallback(entries: IntersectionObserverEntry[]): void {
    entries.forEach((entry): void => {
      if (!entry.isIntersecting) {
        return;
      }

      const spacerBeforeRect = spacerBefore.getBoundingClientRect();
      const spacerAfterRect = spacerAfter.getBoundingClientRect();
      const spacerSeparation = spacerAfterRect.top - spacerBeforeRect.bottom;
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
}

function dispose(dotNetHelper: any): void {
  const observers = observersByDotNetId[dotNetHelper._id];

  if (observers) {
    observers.intersectionObserver.disconnect();
    observers.mutationObserverBefore.disconnect();
    observers.mutationObserverAfter.disconnect();

    dotNetHelper.dispose();

    delete observersByDotNetId[dotNetHelper._id];
  }
}
