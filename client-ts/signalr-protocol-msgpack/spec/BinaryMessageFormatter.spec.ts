// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { BinaryMessageFormat } from "../src/BinaryMessageFormat"

describe("Binary Message Formatter", () => {
    ([
        [[], <Uint8Array[]>[]],
        [[0x00], <Uint8Array[]>[ new Uint8Array([])]],
        [[0x01, 0xff], <Uint8Array[]>[ new Uint8Array([0xff])]],
        [[0x01, 0xff,
          0x01, 0x7f], <Uint8Array[]>[ new Uint8Array([0xff]), new Uint8Array([0x7f])]],
    ] as [number[], Uint8Array[]][]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = BinaryMessageFormat.parse(new Uint8Array(payload).buffer);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        [[0x80], new Error("Cannot read message size.")],
        [[0x02, 0x01, 0x80, 0x80], new Error("Cannot read message size.")],
        [[0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x80], new Error("Cannot read message size.")], // the size of the second message is cut
        [[0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01], new Error("Incomplete message.")], // second message has only size
        [[0xff, 0xff, 0xff, 0xff, 0xff], new Error("Messages bigger than 2GB are not supported.")],
        [[0x80, 0x80, 0x80, 0x80, 0x08], new Error("Messages bigger than 2GB are not supported.")],
        [[0x80, 0x80, 0x80, 0x80, 0x80], new Error("Messages bigger than 2GB are not supported.")],
        [[0x02, 0x00], new Error("Incomplete message.")],
        [[0xff, 0xff, 0xff, 0xff, 0x07], new Error("Incomplete message.")]
    ] as [number[], Error][]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => BinaryMessageFormat.parse(new Uint8Array(payload).buffer)).toThrow(expected_error);
        })
    });

    ([
        [[], [0x00]],
        [[0x20], [0x01, 0x20]],
    ] as [number[], number[]][]).forEach(([input, expected_payload]) => {
        it(`should write '${input}'`, () => {
            let actual = new Uint8Array(BinaryMessageFormat.write(new Uint8Array(input)));
            let expected = new Uint8Array(expected_payload);
            expect(actual).toEqual(expected);
        })
    });

    ([0x0000, 0x0001, 0x007f, 0x0080, 0x3fff, 0x4000, 0xc0de] as number[]).forEach(size => {
        it(`messages should be roundtrippable (message size: '${size}')`, () => {
            const message = [];
            for (let i = 0; i < size; i++) {
                message.push(i & 0xff);
            }

            var payload = new Uint8Array(message);
            expect(payload).toEqual(BinaryMessageFormat.parse(BinaryMessageFormat.write(payload))[0]);
        })
    });

});