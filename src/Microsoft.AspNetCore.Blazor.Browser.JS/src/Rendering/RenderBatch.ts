import { Pointer, System_Array } from '../Platform/Platform';
import { platform } from '../Environment';
import { RenderTreeNodePointer } from './RenderTreeNode';
import { RenderTreeEditPointer } from './RenderTreeEdit';

// Keep in sync with the structs in .NET code

export const renderBatch = {
  updatedComponents: (obj: RenderBatchPointer) => platform.readStructField<ArrayRangePointer<RenderTreeDiffPointer>>(obj, 0),
  disposedComponentIds: (obj: RenderBatchPointer) => platform.readStructField<ArrayRangePointer<number>>(obj, arrayRangeStructLength),
};

const arrayRangeStructLength = 8;
export const arrayRange = {
  array: <T>(obj: ArrayRangePointer<T>) => platform.readObjectField<System_Array<T>>(obj, 0),
  count: <T>(obj: ArrayRangePointer<T>) => platform.readInt32Field(obj, 4),
};

export const renderTreeDiffStructLength = 4 + 2 * arrayRangeStructLength;
export const renderTreeDiff = {
  componentId: (obj: RenderTreeDiffPointer) => platform.readInt32Field(obj, 0),
  edits: (obj: RenderTreeDiffPointer) => platform.readStructField<ArrayRangePointer<RenderTreeEditPointer>>(obj, 4),
  currentState: (obj: RenderTreeDiffPointer) => platform.readStructField<ArrayRangePointer<RenderTreeNodePointer>>(obj, 4 + arrayRangeStructLength),
};

// Nominal types to ensure only valid pointers are passed to the functions above.
// At runtime the values are just numbers.
export interface RenderBatchPointer extends Pointer { RenderBatchPointer__DO_NOT_IMPLEMENT: any }
export interface ArrayRangePointer<T> extends Pointer { ArrayRangePointer__DO_NOT_IMPLEMENT: any }
export interface RenderTreeDiffPointer extends Pointer { RenderTreeDiffPointer__DO_NOT_IMPLEMENT: any }
