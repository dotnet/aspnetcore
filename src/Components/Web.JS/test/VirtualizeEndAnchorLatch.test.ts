import { expect, test, describe, beforeEach } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

// Reproduces the `QuickGrid_AnchorMode_End_PrependAtTop_ViewportStaysStable` E2E failure:
// switching into AnchorMode.End while the viewport is at the top must not jump to the bottom,
// even if the list was momentarily shorter than the viewport while an async ItemsProvider
// was in flight.

function rect(top: number, height: number) {
  return {
    top, height, bottom: top + height, left: 0, right: 0, width: 100, x: 0, y: top,
    toJSON() { return this; },
  };
}

function stubGlobals() {
  (global as any).CSS = { supports: () => true };

  (global as any).IntersectionObserver = class {
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

  (global as any).MutationObserver = class {
    observe() { /* no-op */ }
    disconnect() { /* no-op */ }
    takeRecords() { return []; }
  };

  (global as any).Range.prototype.getBoundingClientRect = () => rect(0, 0);
  (global as any).Range.prototype.getClientRects = () => [];

  (global as any).getComputedStyle = (el: Element) => ({
    overflowY: (el as HTMLElement).style.overflowY || 'visible',
  });
}

const CLIENT_HEIGHT = 300;
const LOADED_SCROLL_HEIGHT = 55078;

function buildDom() {
  document.body.innerHTML = '';

  const container = document.createElement('div');
  container.id = 'qg-anchor-container';
  container.style.overflowY = 'auto';

  const before = document.createElement('div');
  container.appendChild(before);

  const items: HTMLElement[] = [];
  for (let i = 0; i < 6; i++) {
    const item = document.createElement('div');
    item.className = 'item';
    container.appendChild(item);
    items.push(item);
  }

  const after = document.createElement('div');
  container.appendChild(after);
  document.body.appendChild(container);

  // Mutable geometry so the test can model "provider in flight" vs "data loaded".
  const geometry = { scrollHeight: LOADED_SCROLL_HEIGHT, beforeHeight: 0, afterHeight: 54000 };

  Object.defineProperty(container, 'clientHeight', { get: () => CLIENT_HEIGHT, configurable: true });
  Object.defineProperty(container, 'scrollHeight', { get: () => geometry.scrollHeight, configurable: true });
  Object.defineProperty(container, 'clientTop', { value: 0, configurable: true });
  container.getBoundingClientRect = () => rect(0, CLIENT_HEIGHT) as any;

  Object.defineProperty(before, 'offsetHeight', { get: () => geometry.beforeHeight, configurable: true });
  Object.defineProperty(after, 'offsetHeight', { get: () => geometry.afterHeight, configurable: true });
  before.getBoundingClientRect = () => rect(0, geometry.beforeHeight) as any;
  after.getBoundingClientRect = () => rect(CLIENT_HEIGHT, geometry.afterHeight) as any;
  items.forEach((item, i) => {
    item.getBoundingClientRect = () => rect(geometry.beforeHeight + i * 50, 50) as any;
  });

  return { container, before, after, geometry };
}

function createDotNetHelper(): any {
  return { _callDispatcher: {}, _id: 1, invokeMethodAsync: () => Promise.resolve() };
}

const AnchorMode = { None: 0, Start: 1, End: 2 };

describe('Virtualize End anchor mode', () => {
  beforeEach(() => {
    stubGlobals();
  });

  test('switching into End mode at the top does not jump to the bottom after a transient short list', () => {
    const { container, before, after, geometry } = buildDom();
    const helper = createDotNetHelper();

    Virtualize.init(helper, before, after, AnchorMode.None);

    // Data is loaded, user is at the top of a long list.
    container.scrollTop = 0;
    Virtualize.refreshObservers(helper, false);

    // An async ItemsProvider round-trip momentarily leaves nothing rendered, so the
    // container is not scrollable for one render.
    geometry.scrollHeight = CLIENT_HEIGHT;
    geometry.afterHeight = 0;
    Virtualize.refreshObservers(helper, true);

    // The provider resolves; the list is long again and the viewport is still at the top.
    geometry.scrollHeight = LOADED_SCROLL_HEIGHT;
    geometry.afterHeight = 54000;
    container.scrollTop = 0;

    // The user selects AnchorMode.End while parked at the top.
    Virtualize.setAnchorMode(helper, AnchorMode.End);
    Virtualize.refreshObservers(helper, false);

    expect(container.scrollTop).toBe(0);
  });

  test('End mode still follows appends once the viewport has actually reached the bottom', () => {
    const { container, before, after, geometry } = buildDom();
    const helper = createDotNetHelper();

    Virtualize.init(helper, before, after, AnchorMode.End);

    // User scrolls to the bottom of the loaded list.
    container.scrollTop = LOADED_SCROLL_HEIGHT - CLIENT_HEIGHT;
    container.dispatchEvent(new Event('scroll'));
    Virtualize.refreshObservers(helper, false);

    // New items are appended.
    geometry.scrollHeight = LOADED_SCROLL_HEIGHT + 500;
    Virtualize.refreshObservers(helper, false);

    expect(container.scrollTop).toBe(geometry.scrollHeight);
  });
});
