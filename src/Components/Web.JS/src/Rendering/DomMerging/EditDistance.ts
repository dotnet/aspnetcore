type EqualityCallback<T> = (a: T, b: T) => ComparisonResult;

export function compareArrays<T>(before: T[], after: T[], equalityCallback: EqualityCallback<T>): Operation[] {
  // Initialize matrices
  const matrix: number[][] = [];
  const comparisonResults: ComparisonResult[][] = [];
  const beforeLength = before.length;
  const afterLength = after.length;
  if (before.length === 0 && afterLength === 0) {
    return [];
  }

  for (let beforeIndex = 0; beforeIndex <= beforeLength; beforeIndex++) {
    (matrix[beforeIndex] = Array(afterLength + 1))[0] = beforeIndex;
    comparisonResults[beforeIndex] = Array(afterLength + 1);
  }
  const rowZero = matrix[0];
  for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
    rowZero[afterIndex] = afterIndex;
  }

  for (let beforeIndex = 1; beforeIndex <= before.length; beforeIndex++) {
    for (let afterIndex = 1; afterIndex <= after.length; afterIndex++) {
      const beforeItem = before[beforeIndex - 1];
      const afterItem = after[afterIndex - 1];
      const costAsDelete = matrix[beforeIndex - 1][afterIndex] + 1;
      const costAsInsert = matrix[beforeIndex][afterIndex - 1] + 1;

      const comparisonResult = comparisonResults[beforeIndex][afterIndex] = equalityCallback(beforeItem, afterItem);
      if (comparisonResult === ComparisonResult.Incompatible) {
        matrix[beforeIndex][afterIndex] = Math.min(costAsInsert, costAsDelete);
      } else {
        const costAsRetain = matrix[beforeIndex - 1][afterIndex - 1];
        matrix[beforeIndex][afterIndex] = Math.min(costAsInsert, costAsDelete, costAsRetain);
      }
    }
  }

  const operations: Operation[] = [];
  let beforeIndex = before.length;
  let afterIndex = after.length;

  while (beforeIndex > 0 || afterIndex > 0) {
    if (beforeIndex === 0) {
      operations.unshift(Operation.Insert);
      afterIndex--;
    } else if (afterIndex === 0) {
      operations.unshift(Operation.Delete);
      beforeIndex--;
    } else {
      const comparisonResult = comparisonResults[beforeIndex][afterIndex];
      const costAsInsert = matrix[beforeIndex][afterIndex - 1];
      const costAsDelete = matrix[beforeIndex - 1][afterIndex];
      const costAsRetain = comparisonResult === ComparisonResult.Incompatible
        ? Number.MAX_VALUE
        : matrix[beforeIndex - 1][afterIndex - 1];

      if (costAsRetain <= costAsInsert && costAsRetain <= costAsDelete) {
        beforeIndex--;
        afterIndex--;
        operations.unshift(Operation.RetainAsIdentical);
      } else if (costAsInsert < costAsDelete) {
        operations.unshift(Operation.Insert);
        afterIndex--;
      } else {
        operations.unshift(Operation.Delete);
        beforeIndex--;
      }
    }
  }

  return operations;
}

export enum Operation {
  RetainAsIdentical = 0,
  Insert = 2,
  Delete = 3,
}

export enum ComparisonResult {
  Identical = 0,
  Compatible = 1,
  Incompatible = 2,
}
