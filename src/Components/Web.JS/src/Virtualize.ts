export const Virtualize = {
  init,
  dispose,
};

const observersByDotNetId = {};

function findClosestScrollContainer(element: Element | null): Element | null {
  if (!element) {
    return null;
  }

  const style = getComputedStyle(element);

  if (style.overflowY !== 'visible') {
    return element;
  }

  return findClosestScrollContainer(element.parentElement);
}

function init(dotNetHelper: any, spacerBefore: Element, spacerAfter: Element, rootMargin = 50): void {
  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: findClosestScrollContainer(spacerBefore),
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

  function createSpacerMutationObserver(spacer: Element): MutationObserver {
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

      const containerSize = entry.rootBounds?.height;

      if (entry.target === spacerBefore) {
        dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', entry.intersectionRect.top - entry.boundingClientRect.top, containerSize);
      } else if (entry.target === spacerAfter) {
        dotNetHelper.invokeMethodAsync('OnSpacerAfterVisible', entry.boundingClientRect.bottom - entry.intersectionRect.bottom, containerSize);
      } else {
        throw new Error('Unknown intersection target');
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
