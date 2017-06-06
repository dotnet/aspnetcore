
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