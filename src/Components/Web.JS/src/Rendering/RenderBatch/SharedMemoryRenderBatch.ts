// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { platform } from '../../Environment';
import { RenderBatch, ArrayRange, ArrayBuilderSegment, RenderTreeDiff, RenderTreeEdit, RenderTreeFrame, ArrayValues, EditType, FrameType } from './RenderBatch';
import { Pointer, System_Array, System_Object } from '../../Platform/Platform';

// Used when running on Mono WebAssembly for shared-memory interop. The code here encapsulates
// our knowledge of the memory layout of RenderBatch and all referenced types.
//
// In this implementation, all the DTO types are really heap pointers at runtime, hence all
// the casts to 'any' whenever we pass them to platform.read.

export class SharedMemoryRenderBatch implements RenderBatch {
  constructor(private batchAddress: Pointer) {
  }

  // Keep in sync with memory layout in RenderBatch.cs
  updatedComponents(): ArrayRange<RenderTreeDiff> {
    return platform.readStructField<Pointer>(this.batchAddress, 0) as any as ArrayRange<RenderTreeDiff>;
  }

  referenceFrames(): ArrayRange<RenderTreeDiff> {
    return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength) as any as ArrayRange<RenderTreeDiff>;
  }

  disposedComponentIds(): ArrayRange<number> {
    return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength * 2) as any as ArrayRange<number>;
  }

  disposedEventHandlerIds(): ArrayRange<number> {
    return platform.readStructField<Pointer>(this.batchAddress, arrayRangeReader.structLength * 3) as any as ArrayRange<number>;
  }

  updatedComponentsEntry(values: ArrayValues<RenderTreeDiff>, index: number): RenderTreeDiff {
    return arrayValuesEntry(values, index, diffReader.structLength);
  }

  referenceFramesEntry(values: ArrayValues<RenderTreeFrame>, index: number): RenderTreeFrame {
    return arrayValuesEntry(values, index, frameReader.structLength);
  }

  disposedComponentIdsEntry(values: ArrayValues<number>, index: number): number {
    const pointer = arrayValuesEntry(values, index, /* int length */ 4);
    return platform.readInt32Field(pointer as any as Pointer);
  }

  disposedEventHandlerIdsEntry(values: ArrayValues<number>, index: number): number {
    const pointer = arrayValuesEntry(values, index, /* long length */ 8);
    return platform.readUint64Field(pointer as any as Pointer);
  }

  arrayRangeReader = arrayRangeReader;

  arrayBuilderSegmentReader = arrayBuilderSegmentReader;

  diffReader = diffReader;

  editReader = editReader;

  frameReader = frameReader;
}

// Keep in sync with memory layout in ArrayRange.cs
const arrayRangeReader = {
  structLength: 8,
  values: <T>(arrayRange: ArrayRange<T>): ArrayValues<T> => platform.readObjectField<System_Array<T>>(arrayRange as any, 0) as any as ArrayValues<T>,
  count: <T>(arrayRange: ArrayRange<T>): number => platform.readInt32Field(arrayRange as any, 4),
};

// Keep in sync with memory layout in ArrayBuilderSegment
const arrayBuilderSegmentReader = {
  structLength: 12,
  values: <T>(arrayBuilderSegment: ArrayBuilderSegment<T>): ArrayValues<T> => {
    // Evaluate arrayBuilderSegment->_builder->_items, i.e., two dereferences needed
    const builder = platform.readObjectField<System_Object>(arrayBuilderSegment as any, 0);
    const builderFieldsAddress = platform.getObjectFieldsBaseAddress(builder);
    return platform.readObjectField<System_Array<T>>(builderFieldsAddress, 0) as any as ArrayValues<T>;
  },
  offset: <T>(arrayBuilderSegment: ArrayBuilderSegment<T>): number => platform.readInt32Field(arrayBuilderSegment as any, 4),
  count: <T>(arrayBuilderSegment: ArrayBuilderSegment<T>): number => platform.readInt32Field(arrayBuilderSegment as any, 8),
};

// Keep in sync with memory layout in RenderTreeDiff.cs
const diffReader = {
  structLength: 4 + arrayBuilderSegmentReader.structLength,
  componentId: (diff: RenderTreeDiff): number => platform.readInt32Field(diff as any, 0),
  edits: (diff: RenderTreeDiff): ArrayBuilderSegment<RenderTreeEdit> => platform.readStructField<Pointer>(diff as any, 4) as any as ArrayBuilderSegment<RenderTreeEdit>,
  editsEntry: (values: ArrayValues<RenderTreeEdit>, index: number): RenderTreeEdit => arrayValuesEntry(values, index, editReader.structLength),
};

// Keep in sync with memory layout in RenderTreeEdit.cs
const editReader = {
  structLength: 20,
  editType: (edit: RenderTreeEdit): EditType => platform.readInt32Field(edit as any, 0) as EditType,
  siblingIndex: (edit: RenderTreeEdit): number => platform.readInt32Field(edit as any, 4),
  newTreeIndex: (edit: RenderTreeEdit): number => platform.readInt32Field(edit as any, 8),
  moveToSiblingIndex: (edit: RenderTreeEdit): number => platform.readInt32Field(edit as any, 8),
  removedAttributeName: (edit: RenderTreeEdit): string | null => platform.readStringField(edit as any, 16),
};

// Keep in sync with memory layout in RenderTreeFrame.cs
const frameReader = {
  structLength: 36,
  frameType: (frame: RenderTreeFrame): FrameType => platform.readInt16Field(frame as any, 4) as FrameType,
  subtreeLength: (frame: RenderTreeFrame): number => platform.readInt32Field(frame as any, 8),
  elementReferenceCaptureId: (frame: RenderTreeFrame): string | null => platform.readStringField(frame as any, 16),
  componentId: (frame: RenderTreeFrame): number => platform.readInt32Field(frame as any, 12),
  elementName: (frame: RenderTreeFrame): string | null => platform.readStringField(frame as any, 16),
  textContent: (frame: RenderTreeFrame): string | null => platform.readStringField(frame as any, 16),
  markupContent: (frame: RenderTreeFrame): string => platform.readStringField(frame as any, 16)!,
  attributeName: (frame: RenderTreeFrame): string | null => platform.readStringField(frame as any, 16),
  attributeValue: (frame: RenderTreeFrame): string | null => platform.readStringField(frame as any, 24, true),
  attributeEventHandlerId: (frame: RenderTreeFrame): number => platform.readUint64Field(frame as any, 8),
};

function arrayValuesEntry<T>(arrayValues: ArrayValues<T>, index: number, itemSize: number): T {
  return platform.getArrayEntryPtr(arrayValues as any as System_Array<T>, index, itemSize) as any as T;
}
