// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { platform } from '../../Environment';
// Used when running on Mono WebAssembly for shared-memory interop. The code here encapsulates
// our knowledge of the memory layout of RenderBatch and all referenced types.
//
// In this implementation, all the DTO types are really heap pointers at runtime, hence all
// the casts to 'any' whenever we pass them to platform.read.
export class SharedMemoryRenderBatch {
    constructor(batchAddress) {
        this.batchAddress = batchAddress;
        this.arrayRangeReader = arrayRangeReader;
        this.arrayBuilderSegmentReader = arrayBuilderSegmentReader;
        this.diffReader = diffReader;
        this.editReader = editReader;
        this.frameReader = frameReader;
    }
    // Keep in sync with memory layout in RenderBatch.cs
    updatedComponents() {
        return platform.readStructField(this.batchAddress, 0);
    }
    referenceFrames() {
        return platform.readStructField(this.batchAddress, arrayRangeReader.structLength);
    }
    disposedComponentIds() {
        return platform.readStructField(this.batchAddress, arrayRangeReader.structLength * 2);
    }
    disposedEventHandlerIds() {
        return platform.readStructField(this.batchAddress, arrayRangeReader.structLength * 3);
    }
    updatedComponentsEntry(values, index) {
        return arrayValuesEntry(values, index, diffReader.structLength);
    }
    referenceFramesEntry(values, index) {
        return arrayValuesEntry(values, index, frameReader.structLength);
    }
    disposedComponentIdsEntry(values, index) {
        const pointer = arrayValuesEntry(values, index, /* int length */ 4);
        return platform.readInt32Field(pointer);
    }
    disposedEventHandlerIdsEntry(values, index) {
        const pointer = arrayValuesEntry(values, index, /* long length */ 8);
        return platform.readUint64Field(pointer);
    }
}
// Keep in sync with memory layout in ArrayRange.cs
const arrayRangeReader = {
    structLength: 8,
    values: (arrayRange) => platform.readObjectField(arrayRange, 0),
    count: (arrayRange) => platform.readInt32Field(arrayRange, 4),
};
// Keep in sync with memory layout in ArrayBuilderSegment
const arrayBuilderSegmentReader = {
    structLength: 12,
    values: (arrayBuilderSegment) => {
        // Evaluate arrayBuilderSegment->_builder->_items, i.e., two dereferences needed
        const builder = platform.readObjectField(arrayBuilderSegment, 0);
        const builderFieldsAddress = platform.getObjectFieldsBaseAddress(builder);
        return platform.readObjectField(builderFieldsAddress, 0);
    },
    offset: (arrayBuilderSegment) => platform.readInt32Field(arrayBuilderSegment, 4),
    count: (arrayBuilderSegment) => platform.readInt32Field(arrayBuilderSegment, 8),
};
// Keep in sync with memory layout in RenderTreeDiff.cs
const diffReader = {
    structLength: 4 + arrayBuilderSegmentReader.structLength,
    componentId: (diff) => platform.readInt32Field(diff, 0),
    edits: (diff) => platform.readStructField(diff, 4),
    editsEntry: (values, index) => arrayValuesEntry(values, index, editReader.structLength),
};
// Keep in sync with memory layout in RenderTreeEdit.cs
const editReader = {
    structLength: 20,
    editType: (edit) => platform.readInt32Field(edit, 0),
    siblingIndex: (edit) => platform.readInt32Field(edit, 4),
    newTreeIndex: (edit) => platform.readInt32Field(edit, 8),
    moveToSiblingIndex: (edit) => platform.readInt32Field(edit, 8),
    removedAttributeName: (edit) => platform.readStringField(edit, 16),
};
// Keep in sync with memory layout in RenderTreeFrame.cs
const frameReader = {
    structLength: 36,
    frameType: (frame) => platform.readInt16Field(frame, 4),
    subtreeLength: (frame) => platform.readInt32Field(frame, 8),
    elementReferenceCaptureId: (frame) => platform.readStringField(frame, 16),
    componentId: (frame) => platform.readInt32Field(frame, 12),
    elementName: (frame) => platform.readStringField(frame, 16),
    textContent: (frame) => platform.readStringField(frame, 16),
    markupContent: (frame) => platform.readStringField(frame, 16),
    attributeName: (frame) => platform.readStringField(frame, 16),
    attributeValue: (frame) => platform.readStringField(frame, 24, true),
    attributeEventHandlerId: (frame) => platform.readUint64Field(frame, 8),
};
function arrayValuesEntry(arrayValues, index, itemSize) {
    return platform.getArrayEntryPtr(arrayValues, index, itemSize);
}
//# sourceMappingURL=SharedMemoryRenderBatch.js.map