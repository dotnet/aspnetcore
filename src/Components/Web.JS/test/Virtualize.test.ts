import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { Virtualize } from '../src/Virtualize';

const { measureRenderedItems } = Virtualize;

function createDOM(heights: number[]): { before: HTMLDivElement; after: HTMLDivElement } {
  const container = document.createElement('div');
  document.body.appendChild(container);
  const before = document.createElement('div');
  const after = document.createElement('div');
  container.appendChild(before);
  container.appendChild(after);

  for (const h of heights) {
    const item = document.createElement('div');
    item.setAttribute('data-virtualize-item', '');
    item.getBoundingClientRect = () => ({
      height: h, width: 100, top: 0, left: 0, bottom: h, right: 100,
      x: 0, y: 0, toJSON() { return this; },
    });
    container.insertBefore(item, after);
  }

  return { before, after };
}

describe('measureRenderedItems', () => {
  test('returns aggregated sum and count for valid items', () => {
    const { before, after } = createDOM([40, 60]);
    const result = measureRenderedItems(before, after);
    expect(result.heightSum).toBe(100);
    expect(result.heightCount).toBe(2);
  });

  test.each([
    ['zero', [50, 0, 30], 80, 2],
    ['NaN', [50, NaN, 30], 80, 2],
    ['Infinity', [50, Infinity, -Infinity], 50, 1],
    ['negative', [50, -10, 30], 80, 2],
  ])('filters out %s heights before aggregation', (_label, input, expectedSum, expectedCount) => {
    const { before, after } = createDOM(input);
    const result = measureRenderedItems(before, after);
    expect(result.heightSum).toBe(expectedSum);
    expect(result.heightCount).toBe(expectedCount);
  });
});
