import { expect, test, describe } from '@jest/globals';
import { compareArrays, ComparisonResult, ItemList, Operation } from '../src/Rendering/DomMerging/EditDistance';

describe('levenshteinArray', () => {
  test('should empty operations list for empty arrays', () => {
    const before = new ArrayItemList<number>([]);
    const after = new ArrayItemList<number>([]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([]);
  });

  test('should return insertions when all items are added', () => {
    const before = new ArrayItemList<number>([]);
    const after = new ArrayItemList<number>([1, 2, 3]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Insert, Operation.Insert]);
  });

  test('should return deletions when all items are removed', () => {
    const before = new ArrayItemList<number>([1, 2, 3]);
    const after = new ArrayItemList<number>([]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Delete, Operation.Delete, Operation.Delete]);
  });

  test('should return skips for identical arrays', () => {
    const before = new ArrayItemList<number>([1, 2, 3]);
    const after = new ArrayItemList<number>([1, 2, 3]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Skip, Operation.Skip, Operation.Skip]);
  });

  test('should return insert+delete for a replaced item', () => {
    const before = new ArrayItemList<number>([1]);
    const after = new ArrayItemList<number>([2]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Delete]);
  });

  test('should prefer to update rather than insert+delete when allowed', () => {
    const before = new ArrayItemList<number>([1, 2]);
    const after = new ArrayItemList<number>([3, 2]);
    const result = compareArrays(before, after, (a, b) => (a === b) ? ComparisonResult.Identical : ComparisonResult.Compatible);
    expect(result).toEqual([Operation.Update, Operation.Skip]);
  });

  test('should return correct operations for multiple mixed changes', () => {
    const before = new ArrayItemList<number>([1, 2, 3, 4]);
    const after = new ArrayItemList<number>([1, 3, 5, 4]);
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Skip, Operation.Delete, Operation.Skip, Operation.Insert, Operation.Skip]);
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
