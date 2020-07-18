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

function init(component: any, topSpacer: Element, bottomSpacer: Element, rootMargin = 50): void {
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

  mutationObserver.observe(topSpacer, { attributes: true });

  function intersectionCallback(entries: IntersectionObserverEntry[]): void {
    entries.forEach((entry): void => {
      if (!entry.isIntersecting) {
        return;
      }

      const containerSize = entry.rootBounds?.height;

      if (entry.target === topSpacer) {
        component.invokeMethodAsync('OnTopSpacerVisible', entry.intersectionRect.top - entry.boundingClientRect.top, containerSize);
      } else if (entry.target === bottomSpacer) {
        component.invokeMethodAsync('OnBottomSpacerVisible', entry.boundingClientRect.bottom - entry.intersectionRect.bottom, containerSize);
      } else {
        throw new Error('Unknown intersection target');
      }
    });
  }
}

export const Virtualize = {
  init,
};
