import { RenderBatch, ArrayRange, RenderTreeDiff, ArrayValues, RenderTreeEdit, EditType, FrameType, RenderTreeFrame, RenderTreeDiffReader, RenderTreeFrameReader, RenderTreeEditReader, ArrayRangeReader, ArraySegmentReader, ArraySegment } from './RenderBatch';
import { decodeUtf8 } from './Utf8Decoder';

const updatedComponentsEntryLength = 4; // Each is a single int32 giving the location of the data
const referenceFramesEntryLength = 16; // 1 byte for frame type, then 3 bytes for type-specific data
const disposedComponentIdsEntryLength = 4; // Each is an int32 giving the ID
const disposedEventHandlerIdsEntryLength = 4; // Each is an int32 giving the ID
const editsEntryLength = 16; // 4 ints
const stringTableEntryLength = 4; // Each is an int32 giving the string data location, or -1 for null

export class OutOfProcessRenderBatch implements RenderBatch {
  constructor(private batchData: Uint8Array) {
    const stringReader = new OutOfProcessStringReader(batchData);

    this.arrayRangeReader = new OutOfProcessArrayRangeReader(batchData);
    this.arraySegmentReader = new OutOfProcessArraySegmentReader(batchData);
    this.diffReader = new OutOfProcessRenderTreeDiffReader(batchData);
    this.editReader = new OutOfProcessRenderTreeEditReader(batchData, stringReader);
    this.frameReader = new OutOfProcessRenderTreeFrameReader(batchData, stringReader);
  }

  updatedComponents(): ArrayRange<RenderTreeDiff> {
    return readInt32LE(this.batchData, this.batchData.length - 20); // 5th-from-last int32
  }

  referenceFrames(): ArrayRange<RenderTreeFrame> {
    return readInt32LE(this.batchData, this.batchData.length - 16); // 4th-from-last int32
  }

  disposedComponentIds(): ArrayRange<number> {
    return readInt32LE(this.batchData, this.batchData.length - 12); // 3rd-from-last int32
  }

  disposedEventHandlerIds(): ArrayRange<number> {
    return readInt32LE(this.batchData, this.batchData.length - 8); // 2th-from-last int32
  }

  updatedComponentsEntry(values: ArrayValues<RenderTreeDiff>, index: number): RenderTreeDiff {
    const tableEntryPos = (values as any) + index * updatedComponentsEntryLength;
    return readInt32LE(this.batchData, tableEntryPos);
  }

  referenceFramesEntry(values: ArrayValues<RenderTreeFrame>, index: number): RenderTreeFrame {
    return (values as any) + index * referenceFramesEntryLength as any;
  }

  disposedComponentIdsEntry(values: ArrayValues<number>, index: number): number {
    const entryPos = (values as any) + index * disposedComponentIdsEntryLength;
    return readInt32LE(this.batchData, entryPos);
  }

  disposedEventHandlerIdsEntry(values: ArrayValues<number>, index: number): number {
    const entryPos = (values as any) + index * disposedEventHandlerIdsEntryLength;
    return readInt32LE(this.batchData, entryPos);
  }

  diffReader: RenderTreeDiffReader;
  editReader: RenderTreeEditReader;
  frameReader: RenderTreeFrameReader;
  arrayRangeReader: ArrayRangeReader;
  arraySegmentReader: ArraySegmentReader;
}

class OutOfProcessRenderTreeDiffReader implements RenderTreeDiffReader {
  constructor(private batchDataUint8: Uint8Array) {
  }

  componentId(diff: RenderTreeDiff) {
    // First int32 is componentId
    return readInt32LE(this.batchDataUint8, diff as any);
  }

  edits(diff: RenderTreeDiff) {
    // Entries data starts after the componentId (which is a 4-byte int)
    return (diff as any + 4);
  }

  editsEntry(values: ArrayValues<RenderTreeEdit>, index: number) {
    return (values as any) + index * editsEntryLength;
  }
}

class OutOfProcessRenderTreeEditReader implements RenderTreeEditReader {
  constructor(private batchDataUint8: Uint8Array, private stringReader: OutOfProcessStringReader) {
  }

  editType(edit: RenderTreeEdit) {
    return readInt32LE(this.batchDataUint8, edit as any); // 1st int
  }

  siblingIndex(edit: RenderTreeEdit) {
    return readInt32LE(this.batchDataUint8, edit as any + 4); // 2nd int
  }

  newTreeIndex(edit: RenderTreeEdit) {
    return readInt32LE(this.batchDataUint8, edit as any + 8); // 3rd int
  }

  removedAttributeName(edit: RenderTreeEdit) {
    const stringIndex = readInt32LE(this.batchDataUint8, edit as any + 12); // 4th int
    return this.stringReader.readString(stringIndex);
  }
}

class OutOfProcessRenderTreeFrameReader implements RenderTreeFrameReader {
  constructor(private batchDataUint8: Uint8Array, private stringReader: OutOfProcessStringReader) {
  }

  // For render frames, the 2nd-4th ints have different meanings depending on frameType.
  // It's the caller's responsibility not to evaluate properties that aren't applicable to the frameType.

  frameType(frame: RenderTreeFrame) {
    return readInt32LE(this.batchDataUint8, frame as any); // 1st int
  }

  subtreeLength(frame: RenderTreeFrame) {
    return readInt32LE(this.batchDataUint8, frame as any + 4); // 2nd int
  }

  elementReferenceCaptureId(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 4); // 2nd int
    return this.stringReader.readString(stringIndex);
  }

  componentId(frame: RenderTreeFrame) {
    return readInt32LE(this.batchDataUint8, frame as any + 8); // 3rd int
  }

  elementName(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 8); // 3rd int
    return this.stringReader.readString(stringIndex);
  }

  textContent(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 4); // 2nd int
    return this.stringReader.readString(stringIndex);
  }

  markupContent(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 4); // 2nd int
    return this.stringReader.readString(stringIndex)!;
  }

  attributeName(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 4); // 2nd int
    return this.stringReader.readString(stringIndex);
  }

  attributeValue(frame: RenderTreeFrame) {
    const stringIndex = readInt32LE(this.batchDataUint8, frame as any + 8); // 3rd int
    return this.stringReader.readString(stringIndex);
  }

  attributeEventHandlerId(frame: RenderTreeFrame) {
    return readInt32LE(this.batchDataUint8, frame as any + 12); // 4th int
  }
}

class OutOfProcessStringReader {
  private stringTableStartIndex: number;

  constructor(private batchDataUint8: Uint8Array) {
    // Final int gives start position of the string table
    this.stringTableStartIndex = readInt32LE(batchDataUint8, batchDataUint8.length - 4);
  }

  readString(index: number): string | null {
    if (index === -1) { // Special value encodes 'null'
      return null;
    } else {
      const stringTableEntryPos = readInt32LE(this.batchDataUint8, this.stringTableStartIndex + index * stringTableEntryLength);

      // By default, .NET's BinaryWriter gives LEB128-length-prefixed UTF-8 data.
      // This is convenient enough to decode in JavaScript.
      const numUtf8Bytes = readLEB128(this.batchDataUint8, stringTableEntryPos);
      const charsStart = stringTableEntryPos + numLEB128Bytes(numUtf8Bytes);
      const utf8Data = new Uint8Array(
        this.batchDataUint8.buffer,
        this.batchDataUint8.byteOffset + charsStart,
        numUtf8Bytes
      );
      return decodeUtf8(utf8Data);
    }
  }
}

class OutOfProcessArrayRangeReader implements ArrayRangeReader {
  constructor(private batchDataUint8: Uint8Array) {
  }

  count<T>(arrayRange: ArrayRange<T>) {
    // First int is count
    return readInt32LE(this.batchDataUint8, arrayRange as any);
  }

  values<T>(arrayRange: ArrayRange<T>) {
    // Entries data starts after the 'count' int (i.e., after 4 bytes)
    return arrayRange as any + 4;
  }
}

class OutOfProcessArraySegmentReader implements ArraySegmentReader {
  constructor(private batchDataUint8: Uint8Array) {
  }

  offset<T>(arraySegment: ArraySegment<T>) {
    // Not used by the out-of-process representation of RenderBatch data.
    // This only exists on the ArraySegmentReader for the shared-memory representation.
    return 0;
  }

  count<T>(arraySegment: ArraySegment<T>) {
    // First int is count
    return readInt32LE(this.batchDataUint8, arraySegment as any);
  }

  values<T>(arraySegment: ArraySegment<T>): ArrayValues<T> {
    // Entries data starts after the 'count' int (i.e., after 4 bytes)
    return arraySegment as any + 4;
  }
}

function readInt32LE(buffer: Uint8Array, position: number): any {
  return (buffer[position])
    | (buffer[position + 1] << 8)
    | (buffer[position + 2] << 16)
    | (buffer[position + 3] << 24);
}

function readLEB128(buffer: Uint8Array, position: number) {
  let result = 0;
  let shift = 0;
  for (let index = 0; index < 4; index++) {
    const byte = buffer[position + index];
    result |= (byte & 127) << shift;
    if (byte < 128) {
      break;
    }
    shift += 7;
  }
  return result;
}

function numLEB128Bytes(value: number) {
  return value < 128 ? 1
    : value < 16384 ? 2
      : value < 2097152 ? 3 : 4;
}
