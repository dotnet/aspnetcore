import { Message, MessageType } from './Message';

let knownTypes = {
    "T": MessageType.Text,
    "B": MessageType.Binary,
    "C": MessageType.Close,
    "E": MessageType.Error
};

function splitAt(input: string, searchString: string, position: number): [string, number] {
    let index = input.indexOf(searchString, position);
    if (index < 0) {
        return [input.substr(position), input.length];
    }
    let left = input.substring(position, index);
    return [left, index + searchString.length];
}

export namespace ServerSentEventsFormat {
    export function parse(input: string): Message {
        // The SSE protocol is pretty simple. We just look at the first line for the type, and then process the remainder.
        // Binary messages require Base64-decoding and ArrayBuffer support, just like in the other formats below

        if (input.length == 0) {
            throw "Message is missing header";
        }

        let [header, offset] = splitAt(input, "\n", 0);
        let payload = input.substring(offset);

        // Just in case the header used CRLF as the line separator, carve it off
        if (header.endsWith('\r')) {
            header = header.substr(0, header.length - 1);
        }

        // Parse the header
        var messageType = knownTypes[header];
        if (messageType === undefined) {
            throw "Unknown type value: '" + header + "'";
        }

        if (messageType == MessageType.Binary) {
            // We need to decode and put in an ArrayBuffer. Throw for now
            // This will require our own Base64-decoder because the browser
            // built-in one only decodes to strings and throws if invalid UTF-8
            // characters are found.
            throw "TODO: Support for binary messages";
        }

        // Create the message
        return new Message(messageType, payload);
    }
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
            throw `Invalid length: '${lenStr}'`;
        }
        let length = Number.parseInt(lenStr);

        // Required space is: 3 (type flag, ":", ";") + length (payload len)
        if (!hasSpace(input, offset, 3 + length)) {
            throw "Message is incomplete";
        }

        // Read the type
        var [typeStr, offset] = splitAt(input, ":", offset);

        // Parse the type
        var messageType = knownTypes[typeStr];
        if (messageType === undefined) {
            throw "Unknown type value: '" + typeStr + "'";
        }

        // Read the payload
        var payload = input.substr(offset, length);
        offset += length;

        // Verify the final trailing character
        if (input[offset] != ';') {
            throw "Message missing trailer character";
        }
        offset += 1;

        if (messageType == MessageType.Binary) {
            // We need to decode and put in an ArrayBuffer. Throw for now
            // This will require our own Base64-decoder because the browser
            // built-in one only decodes to strings and throws if invalid UTF-8
            // characters are found.
            throw "TODO: Support for binary messages";
        }

        return [offset, new Message(messageType, payload)];
    }

    export function parse(input: string): Message[] {
        if (input.length == 0) {
            return []
        }

        if (input[0] != 'T') {
            throw `Unsupported message format: '${input[0]}'`;
        }

        let messages = [];
        var offset = 1;
        while (offset < input.length) {
            let message;
            [offset, message] = parseMessage(input, offset);
            messages.push(message);
        }
        return messages;
    }
}