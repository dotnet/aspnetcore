// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { decodeUtf8 } from '../../Utf8Decoder';
import { readInt32LE, readUint64LE, readLEB128, numLEB128Bytes } from '../../BinaryDecoder';
const updatedComponentsEntryLength = 4; // Each is a single int32 giving the location of the data
const referenceFramesEntryLength = 20; // 1 int for frame type, then 16 bytes for type-specific data
const disposedComponentIdsEntryLength = 4; // Each is an int32 giving the ID
const disposedEventHandlerIdsEntryLength = 8; // Each is an int64 giving the ID
const editsEntryLength = 16; // 4 ints
const stringTableEntryLength = 4; // Each is an int32 giving the string data location, or -1 for null
export class OutOfProcessRenderBatch {
    constructor(batchData) {
        this.batchData = batchData;
        const stringReader = new OutOfProcessStringReader(batchData);
        this.arrayRangeReader = new OutOfProcessArrayRangeReader(batchData);
        this.arrayBuilderSegmentReader = new OutOfProcessArrayBuilderSegmentReader(batchData);
        this.diffReader = new OutOfProcessRenderTreeDiffReader(batchData);
        this.editReader = new OutOfProcessRenderTreeEditReader(batchData, stringReader);
        this.frameReader = new OutOfProcessRenderTreeFrameReader(batchData, stringReader);
    }
    updatedComponents() {
        return readInt32LE(this.batchData, this.batchData.length - 20); // 5th-from-last int32
    }
    referenceFrames() {
        return readInt32LE(this.batchData, this.batchData.length - 16); // 4th-from-last int32
    }
    disposedComponentIds() {
        return readInt32LE(this.batchData, this.batchData.length - 12); // 3rd-from-last int32
    }
    disposedEventHandlerIds() {
        return readInt32LE(this.batchData, this.batchData.length - 8); // 2th-from-last int32
    }
    updatedComponentsEntry(values, index) {
        const tableEntryPos = values + index * updatedComponentsEntryLength;
        return readInt32LE(this.batchData, tableEntryPos);
    }
    referenceFramesEntry(values, index) {
        return values + index * referenceFramesEntryLength;
    }
    disposedComponentIdsEntry(values, index) {
        const entryPos = values + index * disposedComponentIdsEntryLength;
        return readInt32LE(this.batchData, entryPos);
    }
    disposedEventHandlerIdsEntry(values, index) {
        const entryPos = values + index * disposedEventHandlerIdsEntryLength;
        return readUint64LE(this.batchData, entryPos);
    }
}
class OutOfProcessRenderTreeDiffReader {
    constructor(batchDataUint8) {
        this.batchDataUint8 = batchDataUint8;
    }
    componentId(diff) {
        // First int32 is componentId
        return readInt32LE(this.batchDataUint8, diff);
    }
    edits(diff) {
        // Entries data starts after the componentId (which is a 4-byte int)
        return (diff + 4);
    }
    editsEntry(values, index) {
        return values + index * editsEntryLength;
    }
}
class OutOfProcessRenderTreeEditReader {
    constructor(batchDataUint8, stringReader) {
        this.batchDataUint8 = batchDataUint8;
        this.stringReader = stringReader;
    }
    editType(edit) {
        return readInt32LE(this.batchDataUint8, edit); // 1st int
    }
    siblingIndex(edit) {
        return readInt32LE(this.batchDataUint8, edit + 4); // 2nd int
    }
    newTreeIndex(edit) {
        return readInt32LE(this.batchDataUint8, edit + 8); // 3rd int
    }
    moveToSiblingIndex(edit) {
        return readInt32LE(this.batchDataUint8, edit + 8); // 3rd int
    }
    removedAttributeName(edit) {
        const stringIndex = readInt32LE(this.batchDataUint8, edit + 12); // 4th int
        return this.stringReader.readString(stringIndex);
    }
}
class OutOfProcessRenderTreeFrameReader {
    constructor(batchDataUint8, stringReader) {
        this.batchDataUint8 = batchDataUint8;
        this.stringReader = stringReader;
    }
    // For render frames, the 2nd-4th ints have different meanings depending on frameType.
    // It's the caller's responsibility not to evaluate properties that aren't applicable to the frameType.
    frameType(frame) {
        return readInt32LE(this.batchDataUint8, frame); // 1st int
    }
    subtreeLength(frame) {
        return readInt32LE(this.batchDataUint8, frame + 4); // 2nd int
    }
    elementReferenceCaptureId(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 4); // 2nd int
        return this.stringReader.readString(stringIndex);
    }
    componentId(frame) {
        return readInt32LE(this.batchDataUint8, frame + 8); // 3rd int
    }
    elementName(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 8); // 3rd int
        return this.stringReader.readString(stringIndex);
    }
    textContent(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 4); // 2nd int
        return this.stringReader.readString(stringIndex);
    }
    markupContent(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 4); // 2nd int
        return this.stringReader.readString(stringIndex);
    }
    attributeName(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 4); // 2nd int
        return this.stringReader.readString(stringIndex);
    }
    attributeValue(frame) {
        const stringIndex = readInt32LE(this.batchDataUint8, frame + 8); // 3rd int
        return this.stringReader.readString(stringIndex);
    }
    attributeEventHandlerId(frame) {
        return readUint64LE(this.batchDataUint8, frame + 12); // Bytes 12-19
    }
}
class OutOfProcessStringReader {
    constructor(batchDataUint8) {
        this.batchDataUint8 = batchDataUint8;
        // Final int gives start position of the string table
        this.stringTableStartIndex = readInt32LE(batchDataUint8, batchDataUint8.length - 4);
    }
    readString(index) {
        if (index === -1) { // Special value encodes 'null'
            return null;
        }
        else {
            const stringTableEntryPos = readInt32LE(this.batchDataUint8, this.stringTableStartIndex + index * stringTableEntryLength);
            // By default, .NET's BinaryWriter gives LEB128-length-prefixed UTF-8 data.
            // This is convenient enough to decode in JavaScript.
            const numUtf8Bytes = readLEB128(this.batchDataUint8, stringTableEntryPos);
            const charsStart = stringTableEntryPos + numLEB128Bytes(numUtf8Bytes);
            const utf8Data = new Uint8Array(this.batchDataUint8.buffer, this.batchDataUint8.byteOffset + charsStart, numUtf8Bytes);
            return decodeUtf8(utf8Data);
        }
    }
}
class OutOfProcessArrayRangeReader {
    constructor(batchDataUint8) {
        this.batchDataUint8 = batchDataUint8;
    }
    count(arrayRange) {
        // First int is count
        return readInt32LE(this.batchDataUint8, arrayRange);
    }
    values(arrayRange) {
        // Entries data starts after the 'count' int (i.e., after 4 bytes)
        return arrayRange + 4;
    }
}
class OutOfProcessArrayBuilderSegmentReader {
    constructor(batchDataUint8) {
        this.batchDataUint8 = batchDataUint8;
    }
    offset(_arrayBuilderSegment) {
        // Not used by the out-of-process representation of RenderBatch data.
        // This only exists on the ArrayBuilderSegmentReader for the shared-memory representation.
        return 0;
    }
    count(arrayBuilderSegment) {
        // First int is count
        return readInt32LE(this.batchDataUint8, arrayBuilderSegment);
    }
    values(arrayBuilderSegment) {
        // Entries data starts after the 'count' int (i.e., after 4 bytes)
        return arrayBuilderSegment + 4;
    }
}
//# sourceMappingURL=OutOfProcessRenderBatch.js.map