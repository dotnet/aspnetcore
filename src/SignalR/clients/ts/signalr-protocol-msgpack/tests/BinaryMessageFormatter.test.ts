// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { BinaryMessageFormat } from "../src/BinaryMessageFormat";

describe("Binary Message Formatter", () => {
    ([
        [[], [] as Uint8Array[]],
        [[0x00], [ new Uint8Array([])] as Uint8Array[]],
        [[0x01, 0xff], [ new Uint8Array([0xff])] as Uint8Array[]],
        [[0x01, 0xff,
          0x01, 0x7f], [ new Uint8Array([0xff]), new Uint8Array([0x7f])] as Uint8Array[]],
    ] as [number[], Uint8Array[]][]).forEach(([payload, expectedMessages]) => {
        it(`should parse '${payload}' correctly`, () => {
            const messages = BinaryMessageFormat.parse(new Uint8Array(payload).buffer);
            expect(messages).toEqual(expectedMessages);
        });
    });

    ([
        [[0x80], "Cannot read message size."],
        [[0x02, 0x01, 0x80, 0x80], "Cannot read message size."],
        [[0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80], "Cannot read message size."], // the size of the second message is cut
        [[0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01], "Incomplete message."], // second message has only size
        [[0xff, 0xff, 0xff, 0xff, 0xff], "Messages bigger than 2GB are not supported."],
        [[0x80, 0x80, 0x80, 0x80, 0x08], "Messages bigger than 2GB are not supported."],
        [[0x80, 0x80, 0x80, 0x80, 0x80], "Messages bigger than 2GB are not supported."],
        [[0x02, 0x00], "Incomplete message."],
        [[0xff, 0xff, 0xff, 0xff, 0x07], "Incomplete message."],
    ] as [number[], string][]).forEach(([payload, expectedError]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => BinaryMessageFormat.parse(new Uint8Array(payload).buffer)).toThrow(expectedError);
        });
    });

    ([
        [[], [0x00]],
        [[0x20], [0x01, 0x20]],
    ] as [number[], number[]][]).forEach(([input, expectedPayload]) => {
        it(`should write '${input}'`, () => {
            const actual = new Uint8Array(BinaryMessageFormat.write(new Uint8Array(input)));
            const expected = new Uint8Array(expectedPayload);
            expect(actual).toEqual(expected);
        });
    });

    ([0x0000, 0x0001, 0x007f, 0x0080, 0x3fff, 0x4000, 0xc0de] as number[]).forEach((size) => {
        it(`messages should be roundtrippable (message size: '${size}')`, () => {
            const message = [];
            for (let i = 0; i < size; i++) {
                message.push(i & 0xff);
            }

            const payload = new Uint8Array(message);
            expect(payload).toEqual(BinaryMessageFormat.parse(BinaryMessageFormat.write(payload))[0]);
        });
    });

});
