// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { TextMessageFormat } from "../src/TextMessageFormat"

describe("Text Message Formatter", () => {
    ([
        ["\u001e", [""]],
        ["\u001e\u001e", ["", ""]],
        ["Hello\u001e", ["Hello"]],
        ["Hello,\u001eWorld!\u001e", ["Hello,", "World!"]],
    ] as [string, string[]][]).forEach(([payload, expected_messages]) => {
        it(`should parse '${payload}' correctly`, () => {
            let messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expected_messages);
        })
    });

    ([
        ["", new Error("Message is incomplete.")],
        ["ABC", new Error("Message is incomplete.")],
        ["ABC\u001eXYZ", new Error("Message is incomplete.")],
    ] as [string, Error][]).forEach(([payload, expected_error]) => {
        it(`should fail to parse '${payload}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expected_error);
        });
    });
});