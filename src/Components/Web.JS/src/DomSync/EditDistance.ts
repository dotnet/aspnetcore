// This is just basic Levenshtein. Consider further optimizations such as processing increasingly wide
// diagonal stripes until we find a valid edit sequence. But bear in mind that naively limiting the search
// to a diagonal stripe can produce a different edit script (with the same total cost) as you would get from
// an unbounded search, and it might not retain as many elements.

import { ItemList } from './SiblingSubsetNodeList';

export function getEditScript<T>(before: ItemList<T>, after: ItemList<T>, comparer: (a: T, b: T) => ComparisonResult): Operation[] | null {
  // Initialize matrices
  const matrix: number[][] = [];
  const comparisonResults: ComparisonResult[][] = [];
  const beforeLength = before.length;
  const afterLength = after.length;
  if (before.length === 0 && afterLength === 0) {
    return null;
  }

  for (let beforeIndex = 0; beforeIndex <= beforeLength; beforeIndex++) {
    (matrix[beforeIndex] = Array(afterLength + 1))[0] = beforeIndex;
    comparisonResults[beforeIndex] = Array(afterLength + 1);
  }
  const rowZero = matrix[0];
  for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
    rowZero[afterIndex] = afterIndex;
  }

  // Calculate edit costs
  for (let beforeIndex = 1; beforeIndex <= beforeLength; beforeIndex++) {
    for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
      const beforeItem = before.item(beforeIndex - 1)!;
      const afterItem = after.item(afterIndex - 1)!;
      const costAsInsert = matrix[beforeIndex][afterIndex - 1] + 1;
      const costAsDelete = matrix[beforeIndex - 1][afterIndex] + 1;

      const comparisonResult = comparisonResults[beforeIndex][afterIndex] = comparer(beforeItem, afterItem);
      if (comparisonResult === ComparisonResult.Incompatible) {
        matrix[beforeIndex][afterIndex] = Math.min(costAsInsert, costAsDelete);
      } else {
        // Treat all compatible items as zero cost to edit. The distinction between 'Identical' and 'Compatible'
        // is just so we can select between 'Skip' and 'RetainAndEdit'
        const costAsRetain = matrix[beforeIndex - 1][afterIndex - 1];
        matrix[beforeIndex][afterIndex] = Math.min(costAsInsert, costAsDelete, costAsRetain);
      }
    }
  }

  // Walk through it to select an edit script
  let beforeIndex = beforeLength;
  let afterIndex = afterLength;
  const result: Operation[] = [];
  while (beforeIndex > 0 || afterIndex > 0) {
    if (beforeIndex === 0) {
      // No choice but to insert
      result.unshift(Operation.Insert);
      afterIndex--;
    } else if (afterIndex === 0) {
      // No choice but to delete
      result.unshift(Operation.Delete);
      beforeIndex--;
    } else {
      const comparisonResult = comparisonResults[beforeIndex][afterIndex];
      const costAsInsert = matrix[beforeIndex][afterIndex - 1];
      const costAsDelete = matrix[beforeIndex - 1][afterIndex];
      const costAsRetain = comparisonResult === ComparisonResult.Incompatible
        ? Number.MAX_VALUE
        : matrix[beforeIndex - 1][afterIndex - 1];

      if (costAsRetain <= costAsInsert && costAsRetain <= costAsDelete) {
        const operation = comparisonResult === ComparisonResult.Identical
          ? Operation.RetainAsIdentical
          : Operation.RetainAsCompatible;
        result.unshift(operation);
        beforeIndex--;
        afterIndex--;
      } else if (costAsInsert < costAsDelete) {
        result.unshift(Operation.Insert);
        afterIndex--;
      } else {
        result.unshift(Operation.Delete);
        beforeIndex--;
      }
    }
  }

  //console.log('Compared', before, after);
  //console.log(matrix);
  //console.log(result);
  return result;
}

export const enum Operation {
  RetainAsIdentical = 0,
  RetainAsCompatible = 1,
  Insert = 2,
  Delete = 3,
}

export const enum ComparisonResult {
  Identical = 0,
  Compatible = 1,
  Incompatible = 2,
}
