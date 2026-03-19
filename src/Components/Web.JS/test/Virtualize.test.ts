import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const { measureRenderedItems } = Virtualize;

function createDOM(heights: number[]): { before: HTMLDivElement; after: HTMLDivElement } {
  const container = document.createElement('div');
  document.body.appendChild(container);
  const before = document.createElement('div');
  const after = document.createElement('div');
  container.appendChild(before);

  // Build N+1 fence: comment, item, comment, item, ..., comment
  for (const h of heights) {
    const comment = document.createComment('virtualize:item');
    container.appendChild(comment);
    const item = document.createElement('div');
    item.getBoundingClientRect = () => ({
      height: h, width: 100, top: 0, left: 0, bottom: h, right: 100,
      x: 0, y: 0, toJSON() { return this; },
    });
    container.appendChild(item);
  }
  if (heights.length > 0) {
    container.appendChild(document.createComment('virtualize:item'));
  }

  container.appendChild(after);

  // jsdom doesn't implement Range.getBoundingClientRect.
  // Patch createRange to return a mock that sums item heights between start and end.
  const origCreateRange = document.createRange.bind(document);
  document.createRange = () => {
    const range = origCreateRange();
    let startNode: Node | null = null;
    const origSetStartAfter = range.setStartAfter.bind(range);
    const origSetEndBefore = range.setEndBefore.bind(range);
    range.setStartAfter = (node: Node) => { startNode = node; origSetStartAfter(node); };
    range.setEndBefore = (node: Node) => {
      origSetEndBefore(node);
      // Sum heights of element children between startNode and node
      let totalHeight = 0;
      if (startNode) {
        for (let n = startNode.nextSibling; n && n !== node; n = n.nextSibling) {
          if (n instanceof HTMLElement && n.getBoundingClientRect) {
            totalHeight += n.getBoundingClientRect().height;
          }
        }
      }
      range.getBoundingClientRect = () => ({
        height: totalHeight, width: 100, top: 0, left: 0, bottom: totalHeight, right: 100,
        x: 0, y: 0, toJSON() { return this; },
      } as DOMRect);
    };
    return range;
  };

  return { before, after };
}

describe('measureRenderedItems', () => {
  test('returns aggregated sum and count for valid items', () => {
    const { before, after } = createDOM([40, 60]);
    const result = measureRenderedItems(before, after);
    expect(result.heightSum).toBe(100);
    expect(result.heightCount).toBe(2);
  });

  test('includes zero-height items in count', () => {
    const { before, after } = createDOM([50, 0, 30]);
    const result = measureRenderedItems(before, after);
    // Total height is 80 (50+0+30), all 3 items counted
    expect(result.heightSum).toBe(80);
    expect(result.heightCount).toBe(3);
  });

  test('returns zero for empty item list', () => {
    const { before, after } = createDOM([]);
    const result = measureRenderedItems(before, after);
    expect(result.heightSum).toBe(0);
    expect(result.heightCount).toBe(0);
  });

  test('returns zero for single item (needs at least 2 delimiters)', () => {
    // Single item has 2 delimiters which is the minimum
    const { before, after } = createDOM([42]);
    const result = measureRenderedItems(before, after);
    expect(result.heightSum).toBe(42);
    expect(result.heightCount).toBe(1);
  });
});
