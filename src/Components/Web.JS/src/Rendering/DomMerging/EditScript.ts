// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export interface EditScript {
  skipCount: number,
  edits?: Operation[],
}

export function computeEditScript<T>(before: ItemList<T>, after: ItemList<T>, updateCost: UpdateCostFunc<T>): EditScript {
  // In common cases where nothing has changed or only one thing changed, we can reduce the task dramatically
  // by identifying the common prefix/suffix, and only doing Levenshtein on the subset in between. The end results can entirely
  // ignore any trailing identical entries.
  const commonPrefixLength = lengthOfCommonPrefix(before, after, updateCost);
  if (commonPrefixLength === before.length && commonPrefixLength === after.length) {
    // If by now we know there are no edits, bail out early
    return { skipCount: commonPrefixLength };
  }
  const commonSuffixLength = lengthOfCommonSuffix(before, after, commonPrefixLength, commonPrefixLength, updateCost);
  before = ItemListSubset.create(before, commonPrefixLength, before.length - commonPrefixLength - commonSuffixLength);
  after =  ItemListSubset.create(after, commonPrefixLength, after.length - commonPrefixLength - commonSuffixLength);

  const operations = computeOperations(before, after, updateCost);
  const edits = toEditScript(operations);
  return { skipCount: commonPrefixLength, edits };
}

function lengthOfCommonPrefix<T>(before: ItemList<T>, after: ItemList<T>, updateCost: UpdateCostFunc<T>): number {
  const shorterLength = Math.min(before.length, after.length);
  for (let index = 0; index < shorterLength; index++) {
    if (updateCost(before.item(index)!, after.item(index)!) !== UpdateCost.None) {
      return index;
    }
  }

  return shorterLength;
}

function lengthOfCommonSuffix<T>(before: ItemList<T>, after: ItemList<T>, beforeStartIndex: number, afterStartIndex: number, updateCost: UpdateCostFunc<T>): number {
  let beforeIndex = before.length - 1;
  let afterIndex = after.length - 1;
  let count = 0;
  while (beforeIndex >= beforeStartIndex && afterIndex >= afterStartIndex) {
    if (updateCost(before.item(beforeIndex)!, after.item(afterIndex)!) !== UpdateCost.None) {
      break;
    }
    beforeIndex--;
    afterIndex--;
    count++;
  }
  return count;
}

function computeOperations<T>(before: ItemList<T>, after: ItemList<T>, updateCost: UpdateCostFunc<T>): Operation[][] {
  // Initialize matrices
  const costs: number[][] = [];
  const operations: Operation[][] = [];
  const beforeLength = before.length;
  const afterLength = after.length;
  if (beforeLength === 0 && afterLength === 0) {
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

  for (let beforeIndex = 1; beforeIndex <= beforeLength; beforeIndex++) {
    for (let afterIndex = 1; afterIndex <= afterLength; afterIndex++) {
      const comparisonResult = updateCost(before.item(beforeIndex - 1)!, after.item(afterIndex - 1)!);
      const costAsDelete = costs[beforeIndex - 1][afterIndex] + 1;
      const costAsInsert = costs[beforeIndex][afterIndex - 1] + 1;
      let costAsRetain: number;
      switch (comparisonResult) {
        case UpdateCost.None:
          costAsRetain = costs[beforeIndex - 1][afterIndex - 1];
          break;
        case UpdateCost.Some:
          costAsRetain = costs[beforeIndex - 1][afterIndex - 1] + 1;
          break;
        case UpdateCost.Infinite:
          costAsRetain = Number.MAX_VALUE;
          break;
      }

      if (costAsRetain < costAsInsert && costAsRetain < costAsDelete) {
        costs[beforeIndex][afterIndex] = costAsRetain;
        operations[beforeIndex][afterIndex] = comparisonResult === UpdateCost.None ? Operation.Keep : Operation.Update;
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
      case Operation.Keep:
      case Operation.Update:
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

// Levenshtein naturally deals with these three specific cost values, so they are the only ones allowed.
// If we allowed arbitrary cost numbers, things quickly get very unpredictable.
export enum UpdateCost {
  None,     // Implemented as cost 0
  Some,     // Implemented as cost 1 (same as a single insertion or deletion)
  Infinite, // Implemented as cost infinity (so we would always choose to insert+delete instead of update)
}

type UpdateCostFunc<T> = (a: T, b: T) => UpdateCost;

export enum Operation {
  Keep = 'keep',
  Update = 'update',
  Insert = 'insert',
  Delete = 'delete',
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
