import { expect, test, describe } from '@jest/globals';
import { compareArrays, Operation } from '../src/Rendering/DomMerging/EditDistance';

function exactEqualityComparer<T>(a: T, b: T) {
    return a === b;
}

describe('levenshteinArray', () => {
  test('should return correct operations for empty arrays', () => {
    const before: number[] = [];
    const after: number[] = [];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([]);
  });

  test('should return correct operations for insertions', () => {
    const before: number[] = [];
    const after: number[] = [1, 2, 3];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Insert, Operation.Insert]);
  });

  test('should return correct operations for deletions', () => {
    const before: number[] = [1, 2, 3];
    const after: number[] = [];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Delete, Operation.Delete, Operation.Delete]);
  });

  test('should return correct operations for identical arrays', () => {
    const before: number[] = [1, 2, 3];
    const after: number[] = [1, 2, 3];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Retain, Operation.Retain, Operation.Retain]);
  });

  test('should return correct operations for mixed changes', () => {
    const before: number[] = [1];
    const after: number[] = [2];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Insert, Operation.Delete]);
  });

  test('should return correct operations for multiple mixed changes', () => {
    const before: number[] = [1, 2, 3, 4];
    const after: number[] = [1, 3, 5, 4];
    const result = compareArrays(before, after, exactEqualityComparer);
    expect(result).toEqual([Operation.Retain, Operation.Delete, Operation.Retain, Operation.Insert, Operation.Retain]);
  });
});
