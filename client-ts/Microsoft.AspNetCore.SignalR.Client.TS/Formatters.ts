import { Message, MessageType } from './Message';

let knownTypes = {
    "T": MessageType.Text,
    "B": MessageType.Binary
};

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

    function parseMessage(input: string, position: number): [number, Message] {
        var offset = position;

        // Read the length
        var [lenStr, offset] = splitAt(input, ":", offset);

        // parseInt is too leniant, we need a strict check to see if the string is an int

        if (!LengthRegex.test(lenStr)) {
            throw new Error(`Invalid length: '${lenStr}'`);
        }
        let length = Number.parseInt(lenStr);

        // Required space is: 3 (type flag, ":", ";") + length (payload len)
        if (!hasSpace(input, offset, 3 + length)) {
            throw new Error("Message is incomplete");
        }

        // Read the type
        var [typeStr, offset] = splitAt(input, ":", offset);

        // Parse the type
        var messageType = knownTypes[typeStr];
        if (messageType === undefined) {
            throw new Error(`Unknown type value: '${typeStr}'`);
        }

        // Read the payload
        var payload = input.substr(offset, length);
        offset += length;

        // Verify the final trailing character
        if (input[offset] != ';') {
            throw new Error("Message missing trailer character");
        }
        offset += 1;

        if (messageType == MessageType.Binary) {
            // We need to decode and put in an ArrayBuffer. Throw for now
            // This will require our own Base64-decoder because the browser
            // built-in one only decodes to strings and throws if invalid UTF-8
            // characters are found.
            throw new Error("TODO: Support for binary messages");
        }

        return [offset, new Message(messageType, payload)];
    }

    export function parse(input: string): Message[] {
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