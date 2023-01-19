import { RenderBatch, ArrayRange, ArrayBuilderSegment, RenderTreeDiff, RenderTreeEdit, RenderTreeFrame, ArrayValues, EditType, FrameType } from './RenderBatch';
import { Pointer } from '../../Platform/Platform';
export declare class SharedMemoryRenderBatch implements RenderBatch {
    private batchAddress;
    constructor(batchAddress: Pointer);
    updatedComponents(): ArrayRange<RenderTreeDiff>;
    referenceFrames(): ArrayRange<RenderTreeDiff>;
    disposedComponentIds(): ArrayRange<number>;
    disposedEventHandlerIds(): ArrayRange<number>;
    updatedComponentsEntry(values: ArrayValues<RenderTreeDiff>, index: number): RenderTreeDiff;
    referenceFramesEntry(values: ArrayValues<RenderTreeFrame>, index: number): RenderTreeFrame;
    disposedComponentIdsEntry(values: ArrayValues<number>, index: number): number;
    disposedEventHandlerIdsEntry(values: ArrayValues<number>, index: number): number;
    arrayRangeReader: {
        structLength: number;
        values: <T>(arrayRange: ArrayRange<T>) => ArrayValues<T>;
        count: <T_1>(arrayRange: ArrayRange<T_1>) => number;
    };
    arrayBuilderSegmentReader: {
        structLength: number;
        values: <T>(arrayBuilderSegment: ArrayBuilderSegment<T>) => ArrayValues<T>;
        offset: <T_1>(arrayBuilderSegment: ArrayBuilderSegment<T_1>) => number;
        count: <T_2>(arrayBuilderSegment: ArrayBuilderSegment<T_2>) => number;
    };
    diffReader: {
        structLength: number;
        componentId: (diff: RenderTreeDiff) => number;
        edits: (diff: RenderTreeDiff) => ArrayBuilderSegment<RenderTreeEdit>;
        editsEntry: (values: ArrayValues<RenderTreeEdit>, index: number) => RenderTreeEdit;
    };
    editReader: {
        structLength: number;
        editType: (edit: RenderTreeEdit) => EditType;
        siblingIndex: (edit: RenderTreeEdit) => number;
        newTreeIndex: (edit: RenderTreeEdit) => number;
        moveToSiblingIndex: (edit: RenderTreeEdit) => number;
        removedAttributeName: (edit: RenderTreeEdit) => string | null;
    };
    frameReader: {
        structLength: number;
        frameType: (frame: RenderTreeFrame) => FrameType;
        subtreeLength: (frame: RenderTreeFrame) => number;
        elementReferenceCaptureId: (frame: RenderTreeFrame) => string | null;
        componentId: (frame: RenderTreeFrame) => number;
        elementName: (frame: RenderTreeFrame) => string | null;
        textContent: (frame: RenderTreeFrame) => string | null;
        markupContent: (frame: RenderTreeFrame) => string;
        attributeName: (frame: RenderTreeFrame) => string | null;
        attributeValue: (frame: RenderTreeFrame) => string | null;
        attributeEventHandlerId: (frame: RenderTreeFrame) => number;
    };
}
