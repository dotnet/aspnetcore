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

function init(dotNetHelper: any, topSpacer: Element, bottomSpacer: Element, rootMargin = 50): void {
  const intersectionObserver = new IntersectionObserver(intersectionCallback, {
    root: findClosestScrollContainer(topSpacer),
    rootMargin: `${rootMargin}px`,
  });

  intersectionObserver.observe(topSpacer);
  intersectionObserver.observe(bottomSpacer);

  const mutationObserver = new MutationObserver((): void => {
    intersectionObserver.unobserve(topSpacer);
    intersectionObserver.unobserve(bottomSpacer);
    intersectionObserver.observe(topSpacer);
    intersectionObserver.observe(bottomSpacer);
  });

  // Observe the bottom spacer to account for collections that resize.
  mutationObserver.observe(bottomSpacer, { attributes: true });

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

      if (entry.target === topSpacer) {
        dotNetHelper.invokeMethodAsync('OnTopSpacerVisible', entry.intersectionRect.top - entry.boundingClientRect.top, containerSize);
      } else if (entry.target === bottomSpacer) {
        dotNetHelper.invokeMethodAsync('OnBottomSpacerVisible', entry.boundingClientRect.bottom - entry.intersectionRect.bottom, containerSize);
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
