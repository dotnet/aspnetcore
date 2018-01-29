import { Pointer, System_Array } from '../Platform/Platform';
import { platform } from '../Environment';

// Keep in sync with the structs in .NET code

export const renderBatch = {
  updatedComponents: (obj: RenderBatchPointer) => platform.readStructField(obj, 0) as ArrayRangePointer,
};

const arrayRangeStructLength = 8;
export const arrayRange = {
  array: (obj: ArrayRangePointer) => platform.readObjectField(obj, 0) as System_Array,
  count: (obj: ArrayRangePointer) => platform.readInt32Field(obj, 4),
};

export const renderTreeDiffStructLength = 4 + 2 * arrayRangeStructLength;
export const renderTreeDiff = {
  componentId: (obj: RenderTreeDiffPointer) => platform.readInt32Field(obj, 0),
  edits: (obj: RenderTreeDiffPointer) => platform.readStructField(obj, 4) as ArrayRangePointer,
  currentState: (obj: RenderTreeDiffPointer) => platform.readStructField(obj, 4 + arrayRangeStructLength) as ArrayRangePointer,
};

// Nominal types to ensure only valid pointers are passed to the functions above.
// At runtime the values are just numbers.
export interface RenderBatchPointer extends Pointer { RenderBatchPointer__DO_NOT_IMPLEMENT: any }
export interface ArrayRangePointer extends Pointer { ArrayRangePointer__DO_NOT_IMPLEMENT: any }
export interface RenderTreeDiffPointer extends Pointer { RenderTreeDiffPointer__DO_NOT_IMPLEMENT: any }
