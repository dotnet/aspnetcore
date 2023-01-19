import { RenderBatch, ArrayRange, RenderTreeDiff, ArrayValues, RenderTreeFrame, RenderTreeDiffReader, RenderTreeFrameReader, RenderTreeEditReader, ArrayRangeReader, ArrayBuilderSegmentReader } from './RenderBatch';
export declare class OutOfProcessRenderBatch implements RenderBatch {
    private batchData;
    constructor(batchData: Uint8Array);
    updatedComponents(): ArrayRange<RenderTreeDiff>;
    referenceFrames(): ArrayRange<RenderTreeFrame>;
    disposedComponentIds(): ArrayRange<number>;
    disposedEventHandlerIds(): ArrayRange<number>;
    updatedComponentsEntry(values: ArrayValues<RenderTreeDiff>, index: number): RenderTreeDiff;
    referenceFramesEntry(values: ArrayValues<RenderTreeFrame>, index: number): RenderTreeFrame;
    disposedComponentIdsEntry(values: ArrayValues<number>, index: number): number;
    disposedEventHandlerIdsEntry(values: ArrayValues<number>, index: number): number;
    diffReader: RenderTreeDiffReader;
    editReader: RenderTreeEditReader;
    frameReader: RenderTreeFrameReader;
    arrayRangeReader: ArrayRangeReader;
    arrayBuilderSegmentReader: ArrayBuilderSegmentReader;
}
