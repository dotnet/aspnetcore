export function compareArrays<T>(before: T[], after: T[], equals: (a: T, b: T) => boolean): Operation[] {
  const operations = computeOperations(before, after, equals);
  return toEditScript(operations);
}

function computeOperations<T>(before: T[], after: T[], equals: (a: T, b: T) => boolean): Operation[][] {
  // Initialize matrices
  const costs: number[][] = [];
  const operations: Operation[][] = [];
  const beforeLength = before.length;
  const afterLength = after.length;
  if (before.length === 0 && afterLength === 0) {
    return [];
  }

  for (let beforeIndex = 0; beforeIndex <= beforeLength; beforeIndex++) {
    (costs[beforeIndex] = Array(afterLength + 1))[0] = beforeIndex;
    operations[beforeIndex] = Array(afterLength + 1);
  }
  const rowZero = costs[0];
  for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
    rowZero[afterIndex] = afterIndex;
  }

  for (let beforeIndex = 1; beforeIndex <= before.length; beforeIndex++) {
    for (let afterIndex = 1; afterIndex <= after.length; afterIndex++) {
      const isEqual = equals(before[beforeIndex - 1], after[afterIndex - 1]);
      const costAsDelete = costs[beforeIndex - 1][afterIndex] + 1;
      const costAsInsert = costs[beforeIndex][afterIndex - 1] + 1;
      const costAsRetain = isEqual ? costs[beforeIndex - 1][afterIndex - 1] : Number.MAX_VALUE;

      if (costAsRetain < costAsInsert && costAsRetain < costAsDelete) {
        costs[beforeIndex][afterIndex] = costAsRetain;
        operations[beforeIndex][afterIndex] = Operation.Retain;
      } else if (costAsInsert < costAsDelete) {
        costs[beforeIndex][afterIndex] = costAsInsert;
        operations[beforeIndex][afterIndex] = Operation.Insert;
      } else {
        costs[beforeIndex][afterIndex] = costAsDelete;
        operations[beforeIndex][afterIndex] = Operation.Delete;
      }
    }
  }

  return operations;
}

function toEditScript(operations: Operation[][]) {
  // Start in the bottom-right corner, and work backwards
  const result: Operation[] = [];
  let beforeIndex = operations.length - 1;
  let afterIndex = operations[beforeIndex]?.length - 1;
  while (beforeIndex > 0 || afterIndex > 0) {
    const operation = beforeIndex === 0
      ? Operation.Insert
      : afterIndex === 0
        ? Operation.Delete
        : operations[beforeIndex][afterIndex];

    result.unshift(operation);

    switch (operation) {
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

  return result;
}

export enum Operation {
  Retain = 1,
  Insert = 2,
  Delete = 3,
}
