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

  test('init does not warn for valid spacer elements', () => {
    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
    const originalCss = globalThis.CSS;
    const originalIntersectionObserver = globalThis.IntersectionObserver;
    const originalResizeObserver = globalThis.ResizeObserver;

    mockGlobalProperty('CSS', {
      supports: jest.fn(() => false),
    } as unknown as typeof globalThis.CSS);

    mockGlobalProperty('IntersectionObserver', createNoopObserver() as unknown as typeof globalThis.IntersectionObserver);
    mockGlobalProperty('ResizeObserver', createNoopObserver() as unknown as typeof globalThis.ResizeObserver);

    const scrollContainer = document.createElement('div');
    scrollContainer.style.overflowY = 'auto';
    scrollContainer.append(document.createElement('table'));
    const tbody = document.createElement('tbody');
    const spacerBefore = document.createElement('div');
    const spacerAfter = document.createElement('div');

    tbody.append(spacerBefore, spacerAfter);
    scrollContainer.firstElementChild?.append(tbody);
    document.body.append(scrollContainer);

    const dotNetHelper = {
      _callDispatcher: {},
      _id: 1,
      dispose: () => { },
    };

    try {
      Virtualize.init(dotNetHelper as any, spacerBefore, spacerAfter);
      expect(warnSpy).toHaveBeenCalledWith(
        'Virtualize is rendering inside <table> or <tbody>. Set SpacerElement="tr" to avoid invalid markup.',
      );
    } finally {
      warnSpy.mockRestore();
      scrollContainer.remove();
      mockGlobalProperty('CSS', originalCss as typeof globalThis.CSS);
      mockGlobalProperty('IntersectionObserver', originalIntersectionObserver as typeof globalThis.IntersectionObserver);
      mockGlobalProperty('ResizeObserver', originalResizeObserver as typeof globalThis.ResizeObserver);
    }
  });
});
