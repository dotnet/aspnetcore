import { expect, test, describe } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const createNoopObserver = () => class {
  observe() {}
  unobserve() {}
  disconnect() {}
};

const mockGlobalProperty = <T extends keyof typeof globalThis>(property: T, value: (typeof globalThis)[T]) => {
  Object.defineProperty(globalThis, property, {
    configurable: true,
    value,
  });
};

describe('Virtualize exports', () => {
  test('exports expected functions', () => {
    expect(typeof Virtualize.init).toBe('function');
    expect(typeof Virtualize.dispose).toBe('function');
    expect(typeof Virtualize.scrollToBottom).toBe('function');
    expect(typeof Virtualize.refreshObservers).toBe('function');
    expect(typeof Virtualize.setAnchorMode).toBe('function');
    expect(typeof Virtualize.restoreAnchor).toBe('function');
  });

  function setupTestEnv() {
    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    const originals = {
      CSS: globalThis.CSS,
      IntersectionObserver: globalThis.IntersectionObserver,
      ResizeObserver: globalThis.ResizeObserver,
    };

    mockGlobalProperty('CSS', { supports: jest.fn(() => false) } as any);
    mockGlobalProperty('IntersectionObserver', createNoopObserver() as any);
    mockGlobalProperty('ResizeObserver', createNoopObserver() as any);

    return { warnSpy, originals };
  }

  function cleanupEnv(warnSpy: jest.SpyInstance, originals: any, container: HTMLElement) {
    warnSpy.mockRestore();
    container.remove();
    mockGlobalProperty('CSS', originals.CSS);
    mockGlobalProperty('IntersectionObserver', originals.IntersectionObserver);
    mockGlobalProperty('ResizeObserver', originals.ResizeObserver);
  }

  function createContainer(parentTag: string, spacerTag: string) {
    const container = document.createElement('div');
    container.style.overflowY = 'auto';

    const parent = document.createElement(parentTag);
    const spacerBefore = document.createElement(spacerTag);
    const spacerAfter = document.createElement(spacerTag);

    parent.append(spacerBefore, spacerAfter);
    container.append(parent);
    document.body.append(container);

    return { container, parent, spacerBefore, spacerAfter };
  }

  function createTableContainer(spacerTag: string) {
    const container = document.createElement('div');
    container.style.overflowY = 'auto';

    const table = document.createElement('table');
    const tbody = document.createElement('tbody');

    const spacerBefore = document.createElement(spacerTag);
    const spacerAfter = document.createElement(spacerTag);

    tbody.append(spacerBefore, spacerAfter);
    table.append(tbody);
    container.append(table);
    document.body.append(container);

    return { container, spacerBefore, spacerAfter };
  }

  const dotNetHelper = {
    _callDispatcher: {},
    _id: 1,
    dispose: () => {},
  };

  test.each([
    ['tbody', 'tr'],
    ['ul', 'li'],
  ])('does NOT warn for valid spacer (%s -> %s)', (parent, spacer) => {
    const { warnSpy, originals } = setupTestEnv();

    const { container, spacerBefore, spacerAfter } =
      parent === 'tbody'
        ? createTableContainer(spacer)
        : createContainer(parent, spacer);

    try {
      Virtualize.init(dotNetHelper as any, spacerBefore, spacerAfter);
      expect(warnSpy).not.toHaveBeenCalled();
    } finally {
      cleanupEnv(warnSpy, originals, container);
    }
  });

  test.each([
    ['tbody', 'div', 'tr'],
    ['ul', 'div', 'li'],
    ['ol', 'div', 'li'],
  ])('warns for invalid spacer (%s -> %s)', (parent, spacer, expected) => {
    const { warnSpy, originals } = setupTestEnv();

    const { container, spacerBefore, spacerAfter } =
      parent === 'tbody'
        ? createTableContainer(spacer)
        : createContainer(parent, spacer);

    try {
      Virtualize.init(dotNetHelper as any, spacerBefore, spacerAfter);
      expect(warnSpy).toHaveBeenCalledWith(
        `Virtualize is rendering inside <${parent}>. Set SpacerElement="${expected}" to avoid invalid markup.`,
      );
    } finally {
      cleanupEnv(warnSpy, originals, container);
    }
  });
});
