export function compareArrays<T>(before: ItemList<T>, after: ItemList<T>, equals: (a: T, b: T) => boolean): Operation[] {
  // In common cases where nothing has changed or only one thing changed, we can reduce the task dramatically
  // by identifying the common prefix/suffix, and only doing Levenshtein on the subset in between
  const commonPrefixLength = lengthOfCommonPrefix(before, after, equals);
  const commonSuffixLength = lengthOfCommonSuffix(before, after, commonPrefixLength, commonPrefixLength, equals);
  before = ItemListSubset.create(before, commonPrefixLength, before.length - commonPrefixLength - commonSuffixLength);
  after =  ItemListSubset.create(after, commonPrefixLength, after.length - commonPrefixLength - commonSuffixLength);

  const operations = computeOperations(before, after, equals);
  const edits = toEditScript(operations);

  return Array(commonPrefixLength).fill(Operation.Retain)
    .concat(edits)
    .concat(Array(commonSuffixLength).fill(Operation.Retain));
}

function lengthOfCommonPrefix<T>(before: ItemList<T>, after: ItemList<T>, equals: (a: T, b: T) => boolean): number {
  const shorterLength = Math.min(before.length, after.length);
  for (let index = 0; index < shorterLength; index++) {
    if (!equals(before.item(index)!, after.item(index)!)) {
      return index;
    }
  }

  return shorterLength;
}

function lengthOfCommonSuffix<T>(before: ItemList<T>, after: ItemList<T>, beforeStartIndex: number, afterStartIndex: number, equals: (a: T, b: T) => boolean): number {
  let beforeIndex = before.length - 1;
  let afterIndex = after.length - 1;
  let count = 0;
  while (beforeIndex >= beforeStartIndex && afterIndex >= afterStartIndex) {
    if (!equals(before.item(beforeIndex)!, after.item(afterIndex)!)) {
      break;
    }
    beforeIndex--;
    afterIndex--;
    count++;
  }
  return count;
}

function computeOperations<T>(before: ItemList<T>, after: ItemList<T>, equals: (a: T, b: T) => boolean): Operation[][] {
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
      const isEqual = equals(before.item(beforeIndex - 1)!, after.item(afterIndex - 1)!);
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

export interface ItemList<T> { // Designed to be compatible with NodeList
  readonly length: number;
  item(index: number): T | null;
  forEach(callbackfn: (value: T, key: number, parent: ItemList<T>) => void, thisArg?: any): void;
}

class ItemListSubset<T> implements ItemList<T> {
  static create<T>(source: ItemList<T>, startIndex: number, length: number) {
    return startIndex === 0 && length === source.length
      ? source // No need for a wrapper
      : new ItemListSubset(source, startIndex, length);
  }

  constructor(private source: ItemList<T>, private startIndex: number, public length: number) {
  }
  item(index: number): T | null {
    return this.source.item(index + this.startIndex);
  }
  forEach(callbackfn: (value: T, key: number, parent: ItemList<T>) => void, thisArg?: any): void {
    for (let i = 0; i < this.length; i++) {
      callbackfn(this.item(i)!, i, this);
    }
  }
}
