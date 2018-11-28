import { platform } from '../../Environment';
import { RenderBatch, ArrayRange, ArrayRangeReader, ArraySegment, RenderTreeDiff, RenderTreeEdit, RenderTreeFrame, ArrayValues, EditType, FrameType, RenderTreeFrameReader } from './RenderBatch';
import { Pointer, System_Array } from '../../Platform/Platform';

// Used when running on Mono WebAssembly for shared-memory interop. The code here encapsulates
// our knowledge of the memory layout of RenderBatch and all referenced types.
//
// In this implementation, all the DTO types are really heap pointers at runtime, hence all
// the casts to 'any' whenever we pass them to platform.read.

export class SharedMemoryRenderBatch implements RenderBatch {
  constructor(private batchAddress: Pointer) {
  }

  // Keep in sync with memory layout in RenderBatch.cs
  updatedComponents() { return platform.readStructField<Pointer>(this.batchAddress, 0) as any as ArrayRange<RenderTreeDiff>; }
  referenceFrames() { return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength) as any as ArrayRange<RenderTreeDiff>; }
  disposedComponentIds() { return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength * 2) as any as ArrayRange<number>; }
  disposedEventHandlerIds() { return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength * 3) as any as ArrayRange<number>; }

  updatedComponentsEntry(values: ArrayValues<RenderTreeDiff>, index: number) {
    return arrayValuesEntry(values, index, diffReader.structLength);
  }
  referenceFramesEntry(values: ArrayValues<RenderTreeFrame>, index: number) {
    return arrayValuesEntry(values, index, frameReader.structLength);
  }
  disposedComponentIdsEntry(values: ArrayValues<number>, index: number) {
    const pointer = arrayValuesEntry(values, index, /* int length */ 4);
    return platform.readInt32Field(pointer as any as Pointer);
  }
  disposedEventHandlerIdsEntry(values: ArrayValues<number>, index: number) {
    const pointer = arrayValuesEntry(values, index, /* int length */ 4);
    return platform.readInt32Field(pointer as any as Pointer);
  }

  arrayRangeReader = arrayRangeReader;
  arraySegmentReader = arraySegmentReader;
  diffReader = diffReader;
  editReader = editReader;
  frameReader = frameReader;
}

// Keep in sync with memory layout in ArrayRange.cs
const arrayRangeReader = {
  structLength: 8,
  values: <T>(arrayRange: ArrayRange<T>) => platform.readObjectField<System_Array<T>>(arrayRange as any, 0) as any as ArrayValues<T>,
  count: <T>(arrayRange: ArrayRange<T>) => platform.readInt32Field(arrayRange as any, 4),
};

// Keep in sync with memory layout in ArraySegment
const arraySegmentReader = {
  structLength: 12,
  values: <T>(arraySegment: ArraySegment<T>) => platform.readObjectField<System_Array<T>>(arraySegment as any, 0) as any as ArrayValues<T>,
  offset: <T>(arraySegment: ArraySegment<T>) => platform.readInt32Field(arraySegment as any, 4),
  count: <T>(arraySegment: ArraySegment<T>) => platform.readInt32Field(arraySegment as any, 8),
};

// Keep in sync with memory layout in RenderTreeDiff.cs
const diffReader = {
  structLength: 4 + arraySegmentReader.structLength,
  componentId: (diff: RenderTreeDiff) => platform.readInt32Field(diff as any, 0),
  edits: (diff: RenderTreeDiff) => platform.readStructField<Pointer>(diff as any, 4) as any as ArraySegment<RenderTreeEdit>,
  editsEntry: (values: ArrayValues<RenderTreeEdit>, index: number) => arrayValuesEntry(values, index, editReader.structLength),
};

// Keep in sync with memory layout in RenderTreeEdit.cs
const editReader = {
  structLength: 16,
  editType: (edit: RenderTreeEdit) => platform.readInt32Field(edit as any, 0) as EditType,
  siblingIndex: (edit: RenderTreeEdit) => platform.readInt32Field(edit as any, 4),
  newTreeIndex: (edit: RenderTreeEdit) => platform.readInt32Field(edit as any, 8),
  removedAttributeName: (edit: RenderTreeEdit) => platform.readStringField(edit as any, 12),
};

// Keep in sync with memory layout in RenderTreeFrame.cs
const frameReader = {
  structLength: 28,
  frameType: (frame: RenderTreeFrame) => platform.readInt32Field(frame as any, 4) as FrameType,
  subtreeLength: (frame: RenderTreeFrame) => platform.readInt32Field(frame as any, 8),
  elementReferenceCaptureId: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 16),
  componentId: (frame: RenderTreeFrame) => platform.readInt32Field(frame as any, 12),
  elementName: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 16),
  textContent: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 16),
  markupContent: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 16)!,
  attributeName: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 16),
  attributeValue: (frame: RenderTreeFrame) => platform.readStringField(frame as any, 24),
  attributeEventHandlerId: (frame: RenderTreeFrame) => platform.readInt32Field(frame as any, 8),
};

function arrayValuesEntry<T>(arrayValues: ArrayValues<T>, index: number, itemSize: number): T {
  return platform.getArrayEntryPtr(arrayValues as any as System_Array<T>, index, itemSize) as any as T;
}
