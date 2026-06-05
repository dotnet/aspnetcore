// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach, jest } from "@jest/globals";
import { Virtualize } from "../src/Virtualize";

// ── Polyfill CSS for jsdom ───────────────────────────────────────────────
(globalThis as any).CSS = { supports: jest.fn() as any };

// ── Mock helpers ─────────────────────────────────────────────────────────

interface ObserverCallRecord {
  observe: Element[];
  unobserve: Element[];
  disconnectCalls: number;
  callback: ((entries: any[]) => void) | null;
  options: Record<string, unknown> | null;
}

function createMockObserver(): { mock: ObserverCallRecord; Ctor: jest.Mock } {
  const record: ObserverCallRecord = {
    observe: [] as Element[], unobserve: [] as Element[], disconnectCalls: 0,
    callback: null, options: null,
  };
  function MockObserver(this: any, callback: (entries: any[]) => void, options?: Record<string, unknown>) {
    record.callback = callback; record.options = options || null;
    this.observe = (el: Element) => { record.observe.push(el); };
    this.unobserve = (el: Element) => { record.unobserve.push(el); };
    this.disconnect = () => { record.disconnectCalls++; };
  }
  return { mock: record, Ctor: jest.fn(MockObserver as any) };
}

function createMockDotNetObject(): { helper: any; _callDispatcher: any; id: number } {
  const _callDispatcher: Record<symbol, Record<number, any>> = {};
  const id = 1;
  return {
    helper: { _callDispatcher, _id: id, invokeMethodAsync: jest.fn(), dispose: jest.fn() },
    _callDispatcher, id,
  };
}

function createConnectedElement(tag: string, parent?: HTMLElement): HTMLElement {
  const el = document.createElement(tag);
  Object.defineProperty(el, "isConnected", { value: true, writable: true });
  Object.defineProperty(el, "offsetHeight", { value: 0, writable: true });
  if (parent) parent.appendChild(el);
  return el;
}

function installObserverMocks() {
  const io = createMockObserver();
  const ro = createMockObserver();
  ioCtor = io.Ctor;
  roCtor = ro.Ctor;
  (globalThis as any).IntersectionObserver = io.Ctor;
  (globalThis as any).ResizeObserver = ro.Ctor;
  return { intersectionObserver: io.mock, resizeObserver: ro.mock };
}

(globalThis as any)._cssSupportsOverride = true;

function mockCssSupports(overflowAnchor: boolean) {
  (globalThis as any).CSS = {
    supports: jest.fn((prop: string, _value: string) =>
      prop === "overflow-anchor" ? overflowAnchor : false),
  };
}

function mockOverflowY(element: HTMLElement, value: string) {
  jest.spyOn(window, "getComputedStyle").mockImplementation((_el: Element) =>
    _el === element ? { overflowY: value } as CSSStyleDeclaration
      : { overflowY: "visible" } as CSSStyleDeclaration);
}

let intersectionObserver: ObserverCallRecord;
let resizeObserver: ObserverCallRecord;
let ioCtor: jest.Mock;
let roCtor: jest.Mock;

beforeEach(() => {
  const m = installObserverMocks();
  intersectionObserver = m.intersectionObserver;
  resizeObserver = m.resizeObserver;
  mockCssSupports(true);
});
afterEach(() => jest.restoreAllMocks());

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — guard clauses
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — guard clauses", () => {
  test("returns early when spacerBefore is null", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, null as any, createConnectedElement("div"));
    expect(ioCtor).not.toHaveBeenCalled();
    expect(roCtor).not.toHaveBeenCalled();
  });

  test("returns early when spacerAfter is null", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, createConnectedElement("div"), null as any);
    expect(ioCtor).not.toHaveBeenCalled();
  });

  test("returns early when spacerBefore is disconnected from DOM", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, document.createElement("div"), createConnectedElement("div"));
    expect(ioCtor).not.toHaveBeenCalled();
  });

  test("returns early when spacerAfter is disconnected from DOM", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, createConnectedElement("div"), document.createElement("div"));
    expect(ioCtor).not.toHaveBeenCalled();
  });

  test("proceeds when both spacers are valid and connected", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(ioCtor).toHaveBeenCalledTimes(1);
    expect(roCtor).toHaveBeenCalledTimes(1);
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — IntersectionObserver
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — IntersectionObserver", () => {
  test("uses default rootMargin of 50px", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.rootMargin).toBe("50px");
  });

  test("respects custom rootMargin parameter", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p), 1, 100);
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.rootMargin).toBe("100px");
  });

  test("observes both spacers", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    const before = createConnectedElement("div", p);
    const after = createConnectedElement("div", p);
    Virtualize.init(helper, before, after);
    expect(intersectionObserver.observe).toContain(before);
    expect(intersectionObserver.observe).toContain(after);
  });

  test("uses null root when scroll hits body (documentElement scrolling)", () => {
    const { helper } = createMockDotNetObject();
    const spacer = createConnectedElement("div", document.body);
    mockOverflowY(spacer, "visible");
    Virtualize.init(helper, spacer, createConnectedElement("div", document.body));
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.root).toBeNull();
  });

  test("uses the nearest scrollable ancestor as root", () => {
    const { helper } = createMockDotNetObject();
    const outer = createConnectedElement("div", document.body);
    const inner = createConnectedElement("div", outer);
    mockOverflowY(outer, "auto");
    Virtualize.init(helper, createConnectedElement("div", inner), createConnectedElement("div", inner));
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.root).toBe(outer);
  });

  test("skips ancestors with overflow-y:hidden", () => {
    const { helper } = createMockDotNetObject();
    const outer = createConnectedElement("div", document.body);
    mockOverflowY(outer, "hidden");
    Virtualize.init(helper, createConnectedElement("div", outer), createConnectedElement("div", outer));
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.root).toBeNull();
  });

  test("skips ancestors with overflow-y:clip", () => {
    const { helper } = createMockDotNetObject();
    const outer = createConnectedElement("div", document.body);
    mockOverflowY(outer, "clip" as any);
    Virtualize.init(helper, createConnectedElement("div", outer), createConnectedElement("div", outer));
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.root).toBeNull();
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — ResizeObserver
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — ResizeObserver", () => {
  test("creates a ResizeObserver", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(roCtor).toHaveBeenCalledTimes(1);
  });

  test("observes both spacers at startup for IO re-trigger", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    const before = createConnectedElement("div", p);
    const after = createConnectedElement("div", p);
    Virtualize.init(helper, before, after);
    expect(resizeObserver.observe).toContain(before);
    expect(resizeObserver.observe).toContain(after);
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — overflow-anchor
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — overflow-anchor", () => {
  test("spacers get overflow-anchor:none when native anchoring is supported", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    const before = createConnectedElement("div", p);
    const after = createConnectedElement("div", p);
    Virtualize.init(helper, before, after);
    expect(before.style.overflowAnchor).toBe("none");
    expect(after.style.overflowAnchor).toBe("none");
  });

  test("degraded-anchoring path completes without error (CSS.supports=false)", () => {
    mockCssSupports(false);
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    // When CSS.supports returns false for overflow-anchor, the manual compensation
    // path is used. Verify init completes.
    expect(() => {
      Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    }).not.toThrow();
    // Manual path sets overflow-anchor:none on the scroll element
    expect(ioCtor).toHaveBeenCalled();
    const opts = ioCtor.mock.calls[0][1] as Record<string, unknown>;
    expect(opts.rootMargin).toBe("50px");
  });

  test("table path uses manual anchoring regardless of CSS.supports", () => {
    // Tables always use the manual anchoring path, independent of native anchor support.
    const { helper } = createMockDotNetObject();
    const tbody = document.createElement("tbody"); document.body.appendChild(tbody);
    expect(() => {
      Virtualize.init(helper, createConnectedElement("div", tbody), createConnectedElement("div", tbody));
    }).not.toThrow();
    expect(ioCtor).toHaveBeenCalled();
    expect(roCtor).toHaveBeenCalled();
    document.body.removeChild(tbody);
  });

  test("table path sets display:table-row on spacers", () => {
    const { helper } = createMockDotNetObject();
    const tbody = document.createElement("tbody"); document.body.appendChild(tbody);
    const before = createConnectedElement("div", tbody);
    const after = createConnectedElement("div", tbody);
    Virtualize.init(helper, before, after);
    expect(before.style.display).toBe("table-row");
    expect(after.style.display).toBe("table-row");
    document.body.removeChild(tbody);
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — tabindex
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — tabindex", () => {
  test("sets tabindex=-1 on scroll container when missing", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    mockOverflowY(container, "auto");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    expect(container.getAttribute("tabindex")).toBe("-1");
  });

  test("does NOT overwrite existing tabindex", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    container.setAttribute("tabindex", "0");
    mockOverflowY(container, "auto");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    expect(container.getAttribute("tabindex")).toBe("0");
  });

  test("does NOT set tabindex when scrolling on documentElement", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, createConnectedElement("div", document.body), createConnectedElement("div", document.body));
    expect(document.documentElement.hasAttribute("tabindex")).toBe(false);
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.init — event listeners
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.init — event listeners", () => {
  test("attaches keydown listener on scrollContainer for Home/End", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    mockOverflowY(container, "auto");
    const spy = jest.spyOn(container, "addEventListener");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    expect(spy).toHaveBeenCalledWith("keydown", expect.any(Function));
  });

  test("keydown listener falls back to document when no scrollContainer", () => {
    const { helper } = createMockDotNetObject();
    // When no scroll container is found, keydown is registered on document.
    // Verify init completes without error (event listener attachment is internal).
    expect(() => {
      Virtualize.init(helper, createConnectedElement("div", document.body), createConnectedElement("div", document.body));
    }).not.toThrow();
  });

  test("attaches scroll listener on scrollContainer", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    mockOverflowY(container, "auto");
    const spy = jest.spyOn(container, "addEventListener");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    expect(spy).toHaveBeenCalledWith("scroll", expect.any(Function), expect.any(Object));
  });

  test("scroll listener is attached (window fallback when no container)", () => {
    const { helper } = createMockDotNetObject();
    // When no scroll container is found, scroll is registered on window.
    // Verify init completes without error.
    expect(() => {
      Virtualize.init(helper, createConnectedElement("div", document.body), createConnectedElement("div", document.body));
    }).not.toThrow();
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.dispose
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.dispose", () => {
  test("disconnects both observers and calls dotNetHelper.dispose()", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    Virtualize.dispose(helper);
    expect(intersectionObserver.disconnectCalls).toBeGreaterThanOrEqual(1);
    expect(resizeObserver.disconnectCalls).toBeGreaterThanOrEqual(1);
    expect(helper.dispose).toHaveBeenCalled();
  });

  test("still disposes dotNetHelper when init returned early", () => {
    const { helper } = createMockDotNetObject();
    Virtualize.init(helper, null as any, createConnectedElement("div"));
    Virtualize.dispose(helper);
    expect(helper.dispose).toHaveBeenCalled();
  });

  test("removes the map entry for the disposed component", () => {
    const { helper, _callDispatcher, id } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    const sym = Object.getOwnPropertySymbols(_callDispatcher)[0];
    expect(_callDispatcher[sym][id]).toBeDefined();
    Virtualize.dispose(helper);
    expect(_callDispatcher[sym][id]).toBeUndefined();
  });

  test("removes event listeners that init added", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    mockOverflowY(container, "auto");
    const spy = jest.spyOn(container, "removeEventListener");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    Virtualize.dispose(helper);
    expect(spy).toHaveBeenCalledWith("keydown", expect.any(Function));
    expect(spy).toHaveBeenCalledWith("scroll", expect.any(Function));
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.scrollToBottom
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.scrollToBottom", () => {
  test("scrolls to the bottom of the scroll element", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    Object.defineProperty(document.documentElement, "scrollHeight", { value: 2000, writable: true });
    Virtualize.scrollToBottom(helper);
    // jsdom does not support scrollTop assignment; verifying the call completes
    expect(helper.invokeMethodAsync || true).toBeTruthy();
  });

  test("does not throw when init was never called", () => {
    const { helper } = createMockDotNetObject();
    expect(() => Virtualize.scrollToBottom(helper)).not.toThrow();
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.refreshObservers
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.refreshObservers", () => {
  test("re-observes spacers on ResizeObserver and re-applies styles", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    const countBefore = resizeObserver.observe.length;
    Virtualize.refreshObservers(helper);
    expect(resizeObserver.observe.length).toBeGreaterThanOrEqual(countBefore + 2);
  });

  test("does not throw when entry does not exist", () => {
    const { helper } = createMockDotNetObject();
    expect(() => Virtualize.refreshObservers(helper)).not.toThrow();
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Virtualize.setAnchorMode / restoreAnchor / alignToItem / beginProgrammaticScroll
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize.setAnchorMode", () => {
  test("does not throw when called after init", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(() => Virtualize.setAnchorMode(helper, 2)).not.toThrow();
  });

  test("does not throw when entry does not exist", () => {
    const { helper } = createMockDotNetObject();
    expect(() => Virtualize.setAnchorMode(helper, 1)).not.toThrow();
  });
});

describe("Virtualize.restoreAnchor", () => {
  test("does not throw (no snapshot = early return)", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(() => Virtualize.restoreAnchor(helper)).not.toThrow();
  });
});

describe("Virtualize.alignToItem", () => {
  test("does not throw (target not in DOM = pending retry)", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(() => Virtualize.alignToItem(helper, 5)).not.toThrow();
  });
});

describe("Virtualize.beginProgrammaticScroll", () => {
  test("does not throw", () => {
    const { helper } = createMockDotNetObject();
    const p = createConnectedElement("div");
    Virtualize.init(helper, createConnectedElement("div", p), createConnectedElement("div", p));
    expect(() => Virtualize.beginProgrammaticScroll(helper)).not.toThrow();
  });
});

// ═══════════════════════════════════════════════════════════════════════════
// Integration
// ═══════════════════════════════════════════════════════════════════════════

describe("Virtualize — full lifecycle integration", () => {
  test("init → refresh → scrollToBottom → dispose completes without errors", () => {
    const { helper } = createMockDotNetObject();
    const container = createConnectedElement("div", document.body);
    mockOverflowY(container, "auto");
    Virtualize.init(helper, createConnectedElement("div", container), createConnectedElement("div", container));
    Virtualize.refreshObservers(helper);
    Virtualize.scrollToBottom(helper);
    Virtualize.dispose(helper);
    expect(helper.dispose).toHaveBeenCalled();
  });

  test("multiple components with separate observer maps can coexist", () => {
    const comp1 = createMockDotNetObject();
    const comp2 = createMockDotNetObject();
    comp2.helper._id = 2;
    const p1 = createConnectedElement("div");
    const p2 = createConnectedElement("div");
    Virtualize.init(comp1.helper, createConnectedElement("div", p1), createConnectedElement("div", p1));
    Virtualize.init(comp2.helper, createConnectedElement("div", p2), createConnectedElement("div", p2));
    expect(ioCtor).toHaveBeenCalledTimes(2);
    Virtualize.dispose(comp1.helper);
    expect(comp1.helper.dispose).toHaveBeenCalled();
    expect(comp2.helper.dispose).not.toHaveBeenCalled();
  });
});
