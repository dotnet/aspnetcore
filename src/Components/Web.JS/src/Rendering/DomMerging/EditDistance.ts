type EqualityCallback<T> = (a: T, b: T) => boolean;

export function compareArrays<T>(before: T[], after: T[], equalityCallback: EqualityCallback<T>): Operation[] {
  // Initialize matrices
  const matrix: number[][] = [];
  const chosenOps: Operation[][] = [];
  const beforeLength = before.length;
  const afterLength = after.length;
  if (before.length === 0 && afterLength === 0) {
    return [];
  }

  for (let beforeIndex = 0; beforeIndex <= beforeLength; beforeIndex++) {
    (matrix[beforeIndex] = Array(afterLength + 1))[0] = beforeIndex;
    chosenOps[beforeIndex] = Array(afterLength + 1);
  }
  const rowZero = matrix[0];
  for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
    rowZero[afterIndex] = afterIndex;
  }

  for (let beforeIndex = 1; beforeIndex <= before.length; beforeIndex++) {
    for (let afterIndex = 1; afterIndex <= after.length; afterIndex++) {
      const beforeItem = before[beforeIndex - 1];
      const afterItem = after[afterIndex - 1];
      const isEqual = equalityCallback(beforeItem, afterItem);
      const costAsDelete = matrix[beforeIndex - 1][afterIndex] + 1;
      const costAsInsert = matrix[beforeIndex][afterIndex - 1] + 1;
      const costAsRetain = isEqual ? matrix[beforeIndex - 1][afterIndex - 1] : Number.MAX_VALUE;

      if (costAsRetain < costAsInsert && costAsRetain < costAsDelete) {
        matrix[beforeIndex][afterIndex] = costAsRetain;
        chosenOps[beforeIndex][afterIndex] = Operation.Retain;
      } else if (costAsInsert < costAsDelete) {
        matrix[beforeIndex][afterIndex] = costAsInsert;
        chosenOps[beforeIndex][afterIndex] = Operation.Insert;
      } else {
        matrix[beforeIndex][afterIndex] = costAsDelete;
        chosenOps[beforeIndex][afterIndex] = Operation.Delete;
      }
    }
  }

  const operations: Operation[] = [];
  let beforeIndex = before.length;
  let afterIndex = after.length;

  while (beforeIndex > 0 || afterIndex > 0) {
    const thisOp = beforeIndex === 0
      ? Operation.Insert
      : afterIndex === 0
        ? Operation.Delete
        : chosenOps[beforeIndex][afterIndex];

    operations.unshift(thisOp);

    switch (thisOp) {
      case Operation.Retain:
        beforeIndex--;
        afterIndex--;
        break;
      case Operation.Insert:
        afterIndex--;
        break;
      case Operation.Delete:
        beforeIndex--;
        break;
    }
  }

  return operations;
}

export enum Operation {
  Retain = 0,
  Insert = 1,
  Delete = 2,
}
