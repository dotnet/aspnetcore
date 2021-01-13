export interface RenderBatch {
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

export interface ArrayRangeReader {
  count<T>(arrayRange: ArrayRange<T>): number;
  values<T>(arrayRange: ArrayRange<T>): ArrayValues<T>;
}

export interface ArrayBuilderSegmentReader {
  offset<T>(arrayBuilderSegment: ArrayBuilderSegment<T>): number;
  count<T>(arrayBuilderSegment: ArrayBuilderSegment<T>): number;
  values<T>(arrayBuilderSegment: ArrayBuilderSegment<T>): ArrayValues<T>;
}

export interface RenderTreeDiffReader {
  componentId(diff: RenderTreeDiff): number;
  edits(diff: RenderTreeDiff): ArrayBuilderSegment<RenderTreeEdit>;
  editsEntry(values: ArrayValues<RenderTreeEdit>, index: number): RenderTreeEdit;
}

export interface RenderTreeEditReader {
  editType(edit: RenderTreeEdit): EditType;
  siblingIndex(edit: RenderTreeEdit): number;
  newTreeIndex(edit: RenderTreeEdit): number;
  moveToSiblingIndex(edit: RenderTreeEdit): number;
  removedAttributeName(edit: RenderTreeEdit): string | null;
}

export interface RenderTreeFrameReader {
  frameType(frame: RenderTreeFrame): FrameType;
  subtreeLength(frame: RenderTreeFrame): number;
  elementReferenceCaptureId(frame: RenderTreeFrame): string | null;
  componentId(frame: RenderTreeFrame): number;
  elementName(frame: RenderTreeFrame): string | null;
  textContent(frame: RenderTreeFrame): string | null;
  markupContent(frame: RenderTreeFrame): string;
  attributeName(frame: RenderTreeFrame): string | null;
  attributeValue(frame: RenderTreeFrame): string | null;
  attributeEventHandlerId(frame: RenderTreeFrame): number;
}

export interface ArrayRange<T> { ArrayRange__DO_NOT_IMPLEMENT: any }
export interface ArrayBuilderSegment<T> { ArrayBuilderSegment__DO_NOT_IMPLEMENT: any }
export interface ArrayValues<T> { ArrayValues__DO_NOT_IMPLEMENT: any }

export interface RenderTreeDiff { RenderTreeDiff__DO_NOT_IMPLEMENT: any }
export interface RenderTreeFrame { RenderTreeFrame__DO_NOT_IMPLEMENT: any }
export interface RenderTreeEdit { RenderTreeEdit__DO_NOT_IMPLEMENT: any }

export enum EditType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeEditType.cs
  prependFrame = 1,
  removeFrame = 2,
  setAttribute = 3,
  removeAttribute = 4,
  updateText = 5,
  stepIn = 6,
  stepOut = 7,
  updateMarkup = 8,
  permutationListEntry = 9,
  permutationListEnd = 10,
}

export enum FrameType {
  // The values must be kept in sync with the .NET equivalent in RenderTreeFrameType.cs
  element = 1,
  text = 2,
  attribute = 3,
  component = 4,
  region = 5,
  elementReferenceCapture = 6,
  markup = 8,
}
