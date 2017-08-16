import { TextMessageFormat, BinaryMessageFormat } from "../Microsoft.AspNetCore.SignalR.Client.TS/Formatters"

describe("Text Message Formatter", () => {
    ([
        ["\u001e", [""]],
        ["\u001e\u001e", ["", ""]],
        ["Hello\u001e", ["Hello"]],
        ["Hello,\u001eWorld!\u001e", ["Hello,", "World!"]],
    ] as [[string, string[]]]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        ["", new Error("Message is incomplete.")],
        ["ABC", new Error("Message is incomplete.")],
        ["ABC\u001eXYZ", new Error("Message is incomplete.")],
    ] as [[string, Error]]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expected_error);
        });
    });
});

describe("Binary Message Formatter", () => {
    ([
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], <Uint8Array[]>[ new Uint8Array([])]],
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xff], <Uint8Array[]>[ new Uint8Array([0xff])]],
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xff,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x7f], <Uint8Array[]>[ new Uint8Array([0xff]), new Uint8Array([0x7f])]],
    ] as [[number[], Uint8Array[]]]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = BinaryMessageFormat.parse(new Uint8Array(payload).buffer);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], new Error("Cannot read message size")],
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x01, 0x80, 0x00], new Error("Cannot read message size")],
        [[0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], new Error("Messages bigger than 2147483647 bytes are not supported")],
        [[0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], new Error("Messages bigger than 2147483647 bytes are not supported")],
        [[0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00], new Error("Messages bigger than 2147483647 bytes are not supported")],
        [[0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00], new Error("Messages bigger than 2147483647 bytes are not supported")],
        [[0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00], new Error("Messages bigger than 2147483647 bytes are not supported")],
        [[0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00], new Error("Incomplete message")],
    ] as [[number[], Error]]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => BinaryMessageFormat.parse(new Uint8Array(payload).buffer)).toThrow(expected_error);
        })
    });

    ([
        [[], [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]],
        [[0x20], [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x20]],
    ] as [[number[], number[]]]).forEach(([input, expected_payload]) => {
        it(`should write '${input}'`, () => {
            let actual = new Uint8Array(BinaryMessageFormat.write(new Uint8Array(input)));
            let expected = new Uint8Array(expected_payload);
            expect(actual).toEqual(expected);
        })
    });
});
