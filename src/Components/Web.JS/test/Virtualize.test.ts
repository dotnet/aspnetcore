import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const renderedWindowVersionAttribute = 'data-blazor-virtualize-rendered-window-version';

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

  beforeEach(() => {
    document.body.innerHTML = '';
    spacerSeparation = 600;

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

        observe() {}
        unobserve() {}
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
    jest.restoreAllMocks();
  });

  const invokeMethodAsync = jest.fn();
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
      200,
      2,
      2);
  });

  test('uses viewport height when the document is the scroll root', () => {
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
      900,
      2,
      1);
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
