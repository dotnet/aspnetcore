// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { TextMessageFormat } from "../src/TextMessageFormat";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("TextMessageFormat", () => {
    ([
        ["\u001e", [""]],
        ["\u001e\u001e", ["", ""]],
        ["Hello\u001e", ["Hello"]],
        ["Hello,\u001eWorld!\u001e", ["Hello,", "World!"]],
    ] as [string, string[]][]).forEach(([payload, expectedMessages]) => {
        it(`should parse '${encodeURI(payload)}' correctly`, () => {
            const messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expectedMessages);
        });
    });

    ([
        ["", "Message is incomplete."],
        ["ABC", "Message is incomplete."],
        ["ABC\u001eXYZ", "Message is incomplete."],
    ] as [string, string][]).forEach(([payload, expectedError]) => {
        it(`should fail to parse '${encodeURI(payload)}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expectedError);
        });
    });
});
