// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { TextMessageFormat } from "../src/TextMessageFormat";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("TextMessageFormat", () => {
    ([
        ["\u001e", [""]],
        ["\u001e\u001e", ["", ""]],
        ["Hello\u001e", ["Hello"]],
        ["Hello,\u001eWorld!\u001e", ["Hello,", "World!"]],
    ] as Array<[string, string[]]>).forEach(([payload, expectedMessages]) => {
        it(`should parse '${encodeURI(payload)}' correctly`, () => {
            const messages = TextMessageFormat.parse(payload);
            expect(messages).toEqual(expectedMessages);
        });
    });

    ([
        ["", "Message is incomplete."],
        ["ABC", "Message is incomplete."],
        ["ABC\u001eXYZ", "Message is incomplete."],
    ] as Array<[string, string]>).forEach(([payload, expectedError]) => {
        it(`should fail to parse '${encodeURI(payload)}'`, () => {
            expect(() => TextMessageFormat.parse(payload)).toThrow(expectedError);
        });
    });
});
