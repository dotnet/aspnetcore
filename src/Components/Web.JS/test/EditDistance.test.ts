import { expect, test, describe } from '@jest/globals';
import { compareArrays, ComparisonResult, ItemList, Operation } from '../src/Rendering/DomMerging/EditDistance';

describe('levenshteinArray', () => {
  test('should return correct operations for empty arrays', () => {
    const before = new ArrayItemList<number>([]);
    const after = new ArrayItemList<number>([]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([]);
  });

  test('should return correct operations for insertions', () => {
    const before = new ArrayItemList<number>([]);
    const after = new ArrayItemList<number>([1, 2, 3]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Insert, Operation.Insert]);
  });

  test('should return correct operations for deletions', () => {
    const before = new ArrayItemList<number>([1, 2, 3]);
    const after = new ArrayItemList<number>([]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Delete, Operation.Delete, Operation.Delete]);
  });

  test('should return correct operations for identical arrays', () => {
    const before = new ArrayItemList<number>([1, 2, 3]);
    const after = new ArrayItemList<number>([1, 2, 3]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Retain, Operation.Retain, Operation.Retain]);
  });

  test('should return correct operations for mixed changes', () => {
    const before = new ArrayItemList<number>([1]);
    const after = new ArrayItemList<number>([2]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Delete]);
  });

  test('should return correct operations for multiple mixed changes', () => {
    const before = new ArrayItemList<number>([1, 2, 3, 4]);
    const after = new ArrayItemList<number>([1, 3, 5, 4]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Retain, Operation.Delete, Operation.Retain, Operation.Insert, Operation.Retain]);
  });

  test('should prefer to update rather than insert and delete, when allowed', () => {
    const before = new ArrayItemList<number>([1, 2]);
    const after = new ArrayItemList<number>([3, 2]);
    const result = compareArrays(before, after, (a, b) => (a === b) ? ComparisonResult.Identical : ComparisonResult.Compatible);
    expect(result).toEqual([Operation.Update, Operation.Retain]);
  });
});

function exactEqualityComparer<T>(a: T, b: T) {
  return a === b ? ComparisonResult.Identical : ComparisonResult.Incompatible;
}

class ArrayItemList<T> implements ItemList<T> {
  constructor(private items: T[]) {
    this.length = items.length;
  }

  length: number;

  item(index: number): T | null {
    return this.items[index];
  }

  forEach(callbackfn: (value: T, key: number, parent: ItemList<T>) => void, thisArg?: any): void {
    this.items.forEach((item, key) => callbackfn(item, key, this));
  }
}
