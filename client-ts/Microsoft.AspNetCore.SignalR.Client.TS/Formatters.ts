
function splitAt(input: string, searchString: string, position: number): [string, number] {
    let index = input.indexOf(searchString, position);
    if (index < 0) {
        return [input.substr(position), input.length];
    }
    let left = input.substring(position, index);
    return [left, index + searchString.length];
}

export namespace TextMessageFormat {
    const InvalidPayloadError = new Error("Invalid text message payload");
    const LengthRegex = /^[0-9]+$/;

    function hasSpace(input: string, offset: number, length: number): boolean {
        let requiredLength = offset + length;
        return input.length >= requiredLength;
    }

    function parseMessage(input: string, position: number): [number, string] {
        var offset = position;

        // Read the length
        var [lenStr, offset] = splitAt(input, ":", offset);

        // parseInt is too leniant, we need a strict check to see if the string is an int

        if (!LengthRegex.test(lenStr)) {
            throw new Error(`Invalid length: '${lenStr}'`);
        }
        let length = Number.parseInt(lenStr);

        // Required space is: (";") + length (payload len)
        if (!hasSpace(input, offset, 1 + length)) {
            throw new Error("Message is incomplete");
        }

        // Read the payload
        var payload = input.substr(offset, length);
        offset += length;

        // Verify the final trailing character
        if (input[offset] != ';') {
            throw new Error("Message missing trailer character");
        }
        offset += 1;

        return [offset, payload];
    }

    export function write(output: string): string {
        return `${output.length}:${output};`;
    }

    export function parse(input: string): string[] {
        if (input.length == 0) {
            return []
        }

        let messages = [];
        var offset = 0;
        while (offset < input.length) {
            let message;
            [offset, message] = parseMessage(input, offset);
            messages.push(message);
        }
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
                result.push(uint8Array.slice(offset + 8, offset + 8 + size))
            }
            else {
                throw new Error("Incomplete message");
            }

            offset = offset + 8 + size;
        }

        return result;
    }
}