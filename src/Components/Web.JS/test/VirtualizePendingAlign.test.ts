import { expect, test, describe, beforeEach, afterEach, jest } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const SpacerVisibilityReason = {
  UserScroll: 0,
  ProgrammaticScroll: 1,
  ViewportFill: 2,
  RenderedContentMeasurement: 3,
};

let intersectionCallback: (entries: any[]) => void;

function rect(top: number, height: number) {
  return {
    top, height, bottom: top + height, left: 0, right: 0, width: 100, x: 0, y: top,
    toJSON() { return this; },
  };
}

function stubGlobals() {
  (global as any).CSS = { supports: () => true };

  (global as any).IntersectionObserver = class {
    constructor(cb: (entries: any[]) => void) {
      intersectionCallback = cb;
    }
    observe() { /* no-op */ }
    unobserve() { /* no-op */ }
    disconnect() { /* no-op */ }
    takeRecords() { return []; }
  };

  (global as any).ResizeObserver = class {
    observe() { /* no-op */ }
    unobserve() { /* no-op */ }
    disconnect() { /* no-op */ }
  };

  // jsdom has no layout, so Range reports no geometry.
  (global as any).Range.prototype.getBoundingClientRect = () => rect(127588, 390);
  (global as any).Range.prototype.getClientRects = () => [];

  // jsdom's computed style does not reflect inline overflow-y, which is what
  // findClosestScrollContainer relies on to locate the scroll container.
  (global as any).getComputedStyle = (el: Element) => ({
    overflowY: (el as HTMLElement).style.overflowY || 'visible',
  });
}

// Mirrors the near-end InitialItemIndex layout: a tall before-spacer covering the whole
// viewport, a few rendered items far below it, and an empty after-spacer.
function buildDom() {
  document.body.innerHTML = '';
  const container = document.createElement('div');
  container.id = 'scroll-container';
  container.style.overflowY = 'auto';

  const before = document.createElement('div');
  const after = document.createElement('div');
  container.appendChild(before);

  const items: HTMLElement[] = [];
  for (let i = 0; i < 3; i++) {
    const item = document.createElement('div');
    item.className = 'item';
    container.appendChild(item);
    items.push(item);
  }
  container.appendChild(after);
  document.body.appendChild(container);

  Object.defineProperty(before, 'offsetHeight', { value: 127588, configurable: true });
  Object.defineProperty(after, 'offsetHeight', { value: 0, configurable: true });
  before.getBoundingClientRect = () => rect(0, 127588) as any;
  after.getBoundingClientRect = () => rect(127588, 0) as any;
  items.forEach((item, i) => {
    item.getBoundingClientRect = () => rect(127588 + i * 130, 130) as any;
  });
  container.getBoundingClientRect = () => rect(0, 2000) as any;
  Object.defineProperty(container, 'clientHeight', { value: 2000, configurable: true });
  Object.defineProperty(container, 'scrollHeight', { value: 130000, configurable: true });
  Object.defineProperty(container, 'clientTop', { value: 0, configurable: true });
  container.scrollTo = () => { /* jsdom has no scrolling */ };

  return { container, before, after };
}

function ioEntry(target: Element, isIntersecting: boolean) {
  return {
    target,
    isIntersecting,
    intersectionRect: rect(0, isIntersecting ? 2000 : 0),
    boundingClientRect: target.getBoundingClientRect(),
    rootBounds: rect(0, 2000),
  };
}

function createHelper() {
  return {
    _id: 1,
    _callDispatcher: {},
    invokeMethodAsync: jest.fn(() => Promise.resolve()),
  } as any;
}

function spacerReasons(helper: any): number[] {
  return helper.invokeMethodAsync.mock.calls
    .filter((c: any[]) => c[0] === 'OnSpacerBeforeVisible' || c[0] === 'OnSpacerAfterVisible')
    .map((c: any[]) => c[4] as number);
}

function raiseSpacerIntersection(before: Element, after: Element) {
  intersectionCallback([ioEntry(before, true), ioEntry(after, false)]);
  // Virtualize throttles its intersection callbacks behind a single setTimeout, so flush
  // whatever is pending rather than coupling the test to the throttle duration.
  jest.runOnlyPendingTimers();
}

describe('Virtualize programmatic alignment', () => {
  beforeEach(() => {
    jest.useFakeTimers();
    stubGlobals();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  test('spacer callbacks stay ProgrammaticScroll while an alignment is pending', () => {
    const { container, before, after } = buildDom();
    const helper = createHelper();

    Virtualize.init(helper, before, after);
    Virtualize.beginProgrammaticScroll(helper);

    // The target is outside the committed window, so the alignment defers to a later render
    // and scrollTop has not moved.
    expect(Virtualize.alignToItem(helper, 950) ?? null).toBeNull();
    expect(container.scrollTop).toBe(0);

    raiseSpacerIntersection(before, after);
    helper.invokeMethodAsync.mockClear();

    container.dispatchEvent(new Event('scroll'));
    raiseSpacerIntersection(before, after);

    // ViewportFill and UserScroll both cause C# to move the window or abandon the initial
    // index, which would strand the list at the pre-alignment scroll position.
    const reasons = spacerReasons(helper);
    expect(reasons).not.toContain(SpacerVisibilityReason.ViewportFill);
    expect(reasons).not.toContain(SpacerVisibilityReason.UserScroll);
    expect(reasons).toContain(SpacerVisibilityReason.ProgrammaticScroll);
  });

  test('spacer callbacks resume ViewportFill once the alignment has landed', () => {
    const { before, after } = buildDom();
    const helper = createHelper();

    Virtualize.init(helper, before, after);
    Virtualize.beginProgrammaticScroll(helper);

    // The target is inside the committed window, so the alignment completes immediately.
    expect(Virtualize.alignToItem(helper, 0)).not.toBeNull();

    raiseSpacerIntersection(before, after);
    helper.invokeMethodAsync.mockClear();

    raiseSpacerIntersection(before, after);

    // A completed alignment must still hand control back so viewport-fill can top up the window.
    expect(spacerReasons(helper)).toContain(SpacerVisibilityReason.ViewportFill);
  });
});
