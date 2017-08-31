// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export namespace TextMessageFormat {

    const RecordSeparator = String.fromCharCode(0x1e);

    export function write(output: string): string {
        return `${output}${RecordSeparator}`;
    }

    export function parse(input: string): string[] {
        if (input[input.length - 1] != RecordSeparator) {
            throw new Error("Message is incomplete.");
        }

        let messages = input.split(RecordSeparator);
        messages.pop();
        return messages;
    }
}

export namespace BinaryMessageFormat {
    export function write(output: Uint8Array): ArrayBuffer {
        let size = output.byteLength;
        let buffer = new Uint8Array(size + 8);

        // javascript bitwise operators only support 32-bit integers
        for (let i = 7; i >= 4; i--) {
            buffer[i] = size & 0xff;
            size = size >> 8;
        }

        buffer.set(output, 8);

        return buffer.buffer;
    }

    export function parse(input: ArrayBuffer): Uint8Array[] {
        let result: Uint8Array[] = [];
        let uint8Array = new Uint8Array(input);
        // 8 - the length prefix size
        for (let offset = 0; offset < input.byteLength;) {

            if (input.byteLength < offset + 8) {
                throw new Error("Cannot read message size")
            }

            // Note javascript bitwise operators only support 32-bit integers - for now cutting bigger messages.
            // Tracking bug https://github.com/aspnet/SignalR/issues/613
            if (!(uint8Array[offset] == 0 && uint8Array[offset + 1] == 0 && uint8Array[offset + 2] == 0
                && uint8Array[offset + 3] == 0 && (uint8Array[offset + 4] & 0x80) == 0)) {
                throw new Error("Messages bigger than 2147483647 bytes are not supported");
            }

            let size = 0;
            for (let i = 4; i < 8; i++) {
                size = (size << 8) | uint8Array[offset + i];
            }

            if (uint8Array.byteLength >= (offset + 8 + size)) {
                // IE does not support .slice() so use subarray
                result.push(uint8Array.slice
                    ? uint8Array.slice(offset + 8, offset + 8 + size)
                    : uint8Array.subarray(offset + 8, offset + 8 + size));
            }
            else {
                throw new Error("Incomplete message");
            }

            offset = offset + 8 + size;
        }

        return result;
    }
}