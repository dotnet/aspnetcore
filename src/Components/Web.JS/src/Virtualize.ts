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

function init(dotNetHelper: any, spacerAbove: Element, spacerBelow: Element, rootMargin = 50): void {
  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: findClosestScrollContainer(spacerAbove),
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(spacerAbove);
  intersectionObserver.observe(spacerBelow);

  const mutationObserver = new MutationObserver((): void => {
    intersectionObserver.unobserve(spacerAbove);
    intersectionObserver.unobserve(spacerBelow);
    intersectionObserver.observe(spacerAbove);
    intersectionObserver.observe(spacerBelow);
  });

  // Observe the spacer below to account for collections that resize.
  mutationObserver.observe(spacerAbove, { attributes: true });
  mutationObserver.observe(spacerBelow, { attributes: true });

  observersByDotNetId[dotNetHelper._id] = {
    intersection: intersectionObserver,
    mutation: mutationObserver,
  };

  function intersectionCallback(entries: IntersectionObserverEntry[]): void {
    entries.forEach((entry): void => {
      if (!entry.isIntersecting) {
        return;
      }

      const containerSize = entry.rootBounds?.height;

      if (entry.target === spacerAbove) {
        dotNetHelper.invokeMethodAsync('OnSpacerBeforeVisible', entry.intersectionRect.top - entry.boundingClientRect.top, containerSize);
      } else if (entry.target === spacerBelow) {
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
    observers.intersection.disconnect();
    observers.mutation.disconnect();

    dotNetHelper.dispose();

    delete observersByDotNetId[dotNetHelper._id];
  }
}
