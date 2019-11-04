// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Not exported from index.
/** @private */
export class BinaryMessageFormat {

    // The length prefix of binary messages is encoded as VarInt. Read the comment in
    // the BinaryMessageParser.TryParseMessage for details.

    public static write(output: Uint8Array): ArrayBuffer {
        // msgpack5 uses returns Buffer instead of Uint8Array on IE10 and some other browser
        //  in which case .byteLength does will be undefined
        let size = output.byteLength || output.length;
        const lenBuffer = [];
        do {
            let sizePart = size & 0x7f;
            size = size >> 7;
            if (size > 0) {
                sizePart |= 0x80;
            }
            lenBuffer.push(sizePart);
        }
        while (size > 0);

        // msgpack5 uses returns Buffer instead of Uint8Array on IE10 and some other browser
        //  in which case .byteLength does will be undefined
        size = output.byteLength || output.length;

        const buffer = new Uint8Array(lenBuffer.length + size);
        buffer.set(lenBuffer, 0);
        buffer.set(output, lenBuffer.length);
        return buffer.buffer;
    }

    public static parse(input: ArrayBuffer): Uint8Array[] {
        const result: Uint8Array[] = [];
        const uint8Array = new Uint8Array(input);
        const maxLengthPrefixSize = 5;
        const numBitsToShift = [0, 7, 14, 21, 28 ];

        for (let offset = 0; offset < input.byteLength;) {
            let numBytes = 0;
            let size = 0;
            let byteRead;
            do {
                byteRead = uint8Array[offset + numBytes];
                size = size | ((byteRead & 0x7f) << (numBitsToShift[numBytes]));
                numBytes++;
            }
            while (numBytes < Math.min(maxLengthPrefixSize, input.byteLength - offset) && (byteRead & 0x80) !== 0);

            if ((byteRead & 0x80) !== 0 && numBytes < maxLengthPrefixSize) {
                throw new Error("Cannot read message size.");
            }

            if (numBytes === maxLengthPrefixSize && byteRead > 7) {
                throw new Error("Messages bigger than 2GB are not supported.");
            }

            if (uint8Array.byteLength >= (offset + numBytes + size)) {
                // IE does not support .slice() so use subarray
                result.push(uint8Array.slice
                    ? uint8Array.slice(offset + numBytes, offset + numBytes + size)
                    : uint8Array.subarray(offset + numBytes, offset + numBytes + size));
            } else {
                throw new Error("Incomplete message.");
            }

            offset = offset + numBytes + size;
        }

        return result;
    }
}
