import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const renderedWindowVersionAttribute = 'data-blazor-virtualize-rendered-window-version';
const parameterVersionAttribute = 'data-blazor-virtualize-parameter-version';

describe('Virtualize exports', () => {
  test('exports expected functions', () => {
    expect(typeof Virtualize.init).toBe('function');
    expect(typeof Virtualize.dispose).toBe('function');
    expect(typeof Virtualize.scrollToBottom).toBe('function');
    expect(typeof Virtualize.refreshObservers).toBe('function');
    expect(typeof Virtualize.setAnchorMode).toBe('function');
    expect(typeof Virtualize.restoreAnchor).toBe('function');
  });
});

describe('Virtualize intersection measurements', () => {
  let intersectionCallback: IntersectionObserverCallback;
  let spacerSeparation: number;
  let observe: jest.Mock;
  let unobserve: jest.Mock;

  beforeEach(() => {
    document.body.innerHTML = '';
    spacerSeparation = 600;
    observe = jest.fn();
    unobserve = jest.fn();
    invokeMethodAsync.mockReset().mockResolvedValue(true);

    Object.defineProperty(globalThis, 'CSS', {
      configurable: true,
      value: { supports: () => true },
    });
    Object.defineProperty(globalThis, 'IntersectionObserver', {
      configurable: true,
      value: class {
        constructor(callback: IntersectionObserverCallback) {
          intersectionCallback = callback;
        }

        observe(target: Element) { observe(target); }
        unobserve(target: Element) { unobserve(target); }
        disconnect() {}
      },
    });
    Object.defineProperty(globalThis, 'ResizeObserver', {
      configurable: true,
      value: class {
        observe() {}
        unobserve() {}
        disconnect() {}
      },
    });

    jest.spyOn(document, 'createRange').mockReturnValue({
      setStartAfter() {},
      setEndBefore() {},
      getBoundingClientRect: () => rect(0, spacerSeparation),
    } as unknown as Range);
  });

  afterEach(() => {
    Virtualize.dispose(dotNetHelper);
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  const invokeMethodAsync = jest.fn<(...args: unknown[]) => Promise<boolean>>();
  const dotNetHelper = {
    _callDispatcher: {},
    _id: 1,
    dispose: jest.fn(),
    invokeMethodAsync,
  } as any;

  test('remeasures live DOM when processing a sampled observer entry', () => {
    const container = document.createElement('div');
    container.style.overflowY = 'auto';
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    container.append(spacerBefore, item, spacerAfter);
    document.body.append(container);

    setElementMetrics(container, rect(0, 200), 200);
    setElementMetrics(spacerBefore, rect(-300, 320), 320);
    setElementMetrics(item, rect(20, 50), 50);
    setElementMetrics(spacerAfter, rect(700, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');

    invokeMethodAsync.mockClear();
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);

    const sampledEntry = {
      target: spacerBefore,
      isIntersecting: true,
      boundingClientRect: rect(-300, 320),
      intersectionRect: rect(-50, 70),
      rootBounds: rect(-50, 300),
    } as unknown as IntersectionObserverEntry;

    setElementMetrics(spacerBefore, rect(-100, 120), 120);
    spacerSeparation = 500;
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '2');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '2');

    intersectionCallback([sampledEntry], {} as IntersectionObserver);

    expect(invokeMethodAsync).toHaveBeenCalledWith(
      'OnSpacerBeforeVisible',
      50,
      500,
      300,
      2,
      2);
  });

  test('uses effective viewport height when the document is the scroll root', () => {
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    document.body.append(spacerBefore, item, spacerAfter);

    setElementMetrics(spacerBefore, rect(-100, 120), 120);
    setElementMetrics(item, rect(20, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');
    jest.spyOn(document.documentElement, 'getBoundingClientRect').mockReturnValue(rect(0, 31000));
    jest.spyOn(document.documentElement, 'clientHeight', 'get').mockReturnValue(900);

    invokeMethodAsync.mockClear();
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);

    intersectionCallback([{
      target: spacerBefore,
      isIntersecting: true,
    } as unknown as IntersectionObserverEntry], {} as IntersectionObserver);

    expect(invokeMethodAsync).toHaveBeenCalledWith(
      'OnSpacerBeforeVisible',
      50,
      600,
      1000,
      2,
      1);
  });

  test('uses effective viewport height when aligning with the document as the scroll root', () => {
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    document.body.append(spacerBefore, item, spacerAfter);

    setElementMetrics(spacerBefore, rect(-100, 100), 100);
    setElementMetrics(item, rect(0, 50), 50);
    setElementMetrics(spacerAfter, rect(1100, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');
    jest.spyOn(document.documentElement, 'getBoundingClientRect').mockReturnValue(rect(0, 31000));
    jest.spyOn(document.documentElement, 'clientHeight', 'get').mockReturnValue(900);

    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);

    expect(Virtualize.alignToItem(dotNetHelper, 0)).toEqual({
      fillDirection: 0,
      spacerSeparation: 600,
      containerSize: 1000,
      renderedWindowVersion: 1,
    });
  });

  test('clears alignment state when deferred targets no longer intersect', () => {
    jest.useFakeTimers();
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    document.body.append(spacerBefore, item, spacerAfter);

    setElementMetrics(spacerBefore, rect(-10, 20), 20);
    setElementMetrics(item, rect(10, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');
    jest.spyOn(document.documentElement, 'clientHeight', 'get').mockReturnValue(300);

    invokeMethodAsync.mockClear();
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter, 0);
    const entry = {
      target: spacerBefore,
      isIntersecting: true,
    } as unknown as IntersectionObserverEntry;
    intersectionCallback([entry], {} as IntersectionObserver);
    invokeMethodAsync.mockClear();

    Virtualize.beginProgrammaticScroll(dotNetHelper);
    setElementMetrics(spacerBefore, rect(-200, 20), 20);
    intersectionCallback([entry], {} as IntersectionObserver);
    jest.advanceTimersByTime(50);

    setElementMetrics(spacerBefore, rect(-10, 20), 20);
    intersectionCallback([entry], {} as IntersectionObserver);

    expect(invokeMethodAsync).toHaveBeenCalledWith(
      'OnSpacerBeforeVisible',
      0,
      600,
      400,
      2,
      1);
  });

  test('dispatches callback-time measurement after the target moves during throttling', () => {
    jest.useFakeTimers();
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    document.body.append(spacerBefore, item, spacerAfter);

    setElementMetrics(spacerBefore, rect(-10, 20), 20);
    setElementMetrics(item, rect(10, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');
    jest.spyOn(document.documentElement, 'clientHeight', 'get').mockReturnValue(300);

    invokeMethodAsync.mockClear();
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter, 0);
    const entry = {
      target: spacerBefore,
      isIntersecting: true,
    } as unknown as IntersectionObserverEntry;
    intersectionCallback([entry], {} as IntersectionObserver);
    invokeMethodAsync.mockClear();

    setElementMetrics(spacerBefore, rect(-100, 120), 120);
    spacerSeparation = 500;
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '2');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '2');
    intersectionCallback([entry], {} as IntersectionObserver);

    setElementMetrics(spacerBefore, rect(-200, 20), 20);
    spacerSeparation = 400;
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '3');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '3');
    jest.advanceTimersByTime(50);

    expect(invokeMethodAsync).toHaveBeenCalledWith(
      'OnSpacerBeforeVisible',
      50,
      500,
      400,
      2,
      2);
  });

  test.each([
    ['edge-adjacent nonzero spacer', -60, 10, true],
    ['separated spacer', -61, 10, false],
    ['edge-adjacent zero-height spacer', -50, 0, true],
    ['overlapping spacer', -59, 10, true],
  ])('matches threshold-zero intersection semantics for %s', (_, top, height, expectedCallback) => {
    const container = document.createElement('div');
    container.style.overflowY = 'auto';
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    container.append(spacerBefore, item, spacerAfter);
    document.body.append(container);

    setElementMetrics(container, rect(0, 200), 200);
    setElementMetrics(spacerBefore, rect(top, height), height);
    setElementMetrics(item, rect(20, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');

    invokeMethodAsync.mockClear();
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);
    intersectionCallback([{
      target: spacerBefore,
      isIntersecting: expectedCallback,
    } as unknown as IntersectionObserverEntry], {} as IntersectionObserver);

    expect(invokeMethodAsync).toHaveBeenCalledTimes(expectedCallback ? 1 : 0);
  });

  test('reobserves spacers after a parameter-driven render', async () => {
    const container = document.createElement('div');
    container.style.overflowY = 'auto';
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    container.append(spacerBefore, item, spacerAfter);
    document.body.append(container);

    setElementMetrics(container, rect(0, 200), 200);
    setElementMetrics(spacerBefore, rect(-10, 20), 20);
    setElementMetrics(item, rect(10, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');
    spacerBefore.setAttribute(parameterVersionAttribute, '1');
    spacerAfter.setAttribute(parameterVersionAttribute, '1');

    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);
    observe.mockClear();
    unobserve.mockClear();

    spacerBefore.setAttribute(renderedWindowVersionAttribute, '2');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '2');
    await Promise.resolve();

    expect(unobserve).not.toHaveBeenCalled();
    expect(observe).not.toHaveBeenCalled();

    spacerBefore.setAttribute(parameterVersionAttribute, '2');
    spacerAfter.setAttribute(parameterVersionAttribute, '2');
    await Promise.resolve();

    expect(unobserve).toHaveBeenCalledTimes(2);
    expect(observe).toHaveBeenCalledTimes(2);
  });

  test('reobserves spacers when managed code rejects a stale measurement', async () => {
    jest.useFakeTimers();
    const container = document.createElement('div');
    container.style.overflowY = 'auto';
    const spacerBefore = document.createElement('div');
    const item = document.createElement('div');
    const spacerAfter = document.createElement('div');
    spacerBefore.style.overflowY = 'visible';
    container.append(spacerBefore, item, spacerAfter);
    document.body.append(container);

    setElementMetrics(container, rect(0, 200), 200);
    setElementMetrics(spacerBefore, rect(-10, 20), 20);
    setElementMetrics(item, rect(10, 50), 50);
    setElementMetrics(spacerAfter, rect(1000, 100), 100);
    spacerBefore.setAttribute(renderedWindowVersionAttribute, '1');
    spacerAfter.setAttribute(renderedWindowVersionAttribute, '1');

    invokeMethodAsync.mockReset().mockResolvedValueOnce(false);
    Virtualize.init(dotNetHelper, spacerBefore, spacerAfter);
    observe.mockClear();
    unobserve.mockClear();

    const entry = {
      target: spacerBefore,
      isIntersecting: true,
    } as unknown as IntersectionObserverEntry;
    intersectionCallback([entry], {} as IntersectionObserver);
    await Promise.resolve();

    expect(unobserve).toHaveBeenCalledTimes(2);
    expect(observe).toHaveBeenCalledTimes(2);

    jest.advanceTimersByTime(50);
    observe.mockClear();
    unobserve.mockClear();
    invokeMethodAsync.mockResolvedValueOnce(true);
    intersectionCallback([entry], {} as IntersectionObserver);
    await Promise.resolve();

    expect(unobserve).not.toHaveBeenCalled();
    expect(observe).not.toHaveBeenCalled();
  });
});

function rect(top: number, height: number): DOMRect {
  return {
    x: 0,
    y: top,
    width: 100,
    height,
    top,
    right: 100,
    bottom: top + height,
    left: 0,
    toJSON() {},
  };
}

function setElementMetrics(element: HTMLElement, elementRect: DOMRect, height: number): void {
  element.getBoundingClientRect = () => elementRect;
  Object.defineProperty(element, 'offsetHeight', { configurable: true, value: height });
  Object.defineProperty(element, 'clientHeight', { configurable: true, value: height });
}
