// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { MessagePackHubProtocol } from "../src/MessagePackHubProtocol"
import { MessageType, InvocationMessage, CompletionMessage, ResultMessage } from "@aspnet/signalr"

describe("MessageHubProtocol", () => {
    it("can write/read non-blocking Invocation message", () => {
        let invocation = <InvocationMessage>{
            type: MessageType.Invocation,
            target: "myMethod",
            arguments: [42, true, "test", ["x1", "y2"], null]
        };

        let protocol = new MessagePackHubProtocol();
        var parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation));
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message", () => {
        let invocation = <InvocationMessage>{
            type: MessageType.Invocation,
            invocationId: "123",
            target: "myMethod",
            arguments: [42, true, "test", ["x1", "y2"], null]
        };

        let protocol = new MessagePackHubProtocol();
        var parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation));
        expect(parsedMessages).toEqual([invocation]);
    });

    ([
        [[0x0b, 0x94, 0x03, 0xa3, 0x61, 0x62, 0x63, 0x01, 0xa3, 0x45, 0x72, 0x72],
        {
            type: MessageType.Completion,
            invocationId: "abc",
            error: "Err",
            result: null
        } as CompletionMessage],
        [[0x0a, 0x94, 0x03, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b],
        {
            type: MessageType.Completion,
            invocationId: "abc",
            error: null,
            result: "OK"
        } as CompletionMessage],
        [[0x07, 0x93, 0x03, 0xa3, 0x61, 0x62, 0x63, 0x02],
        {
            type: MessageType.Completion,
            invocationId: "abc",
            error: null,
            result: null
        } as CompletionMessage]
    ] as [number[], CompletionMessage][]).forEach(([payload, expected_message]) =>
        it("can read Completion message", () => {
            let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
            expect(messages).toEqual([expected_message]);
        }));

    ([
        [[0x07, 0x93, 0x02, 0xa3, 0x61, 0x62, 0x63, 0x08],
        {
            type: MessageType.StreamItem,
            invocationId: "abc",
            item: 8
        } as ResultMessage]
    ] as [[number[], CompletionMessage]]).forEach(([payload, expected_message]) =>
        it("can read Result message", () => {
            let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
            expect(messages).toEqual([expected_message]);
        }));

    ([
        [[0x00], new Error("Invalid payload.")],
        [[0x01, 0x90], new Error("Invalid payload.")],
        [[0x01, 0xc2], new Error("Invalid payload.")],
        [[0x02, 0x91, 0x05], new Error("Invalid message type.")],
        [[0x03, 0x91, 0xa1, 0x78], new Error("Invalid message type.")],
        [[0x02, 0x91, 0x01], new Error("Invalid payload for Invocation message.")],
        [[0x02, 0x91, 0x02], new Error("Invalid payload for stream Result message.")],
        [[0x03, 0x92, 0x03, 0xa0], new Error("Invalid payload for Completion message.")],
        [[0x05, 0x94, 0x03, 0xa0, 0x02, 0x00], new Error("Invalid payload for Completion message.")],
        [[0x04, 0x93, 0x03, 0xa0, 0x01], new Error("Invalid payload for Completion message.")],
        [[0x04, 0x93, 0x03, 0xa0, 0x03], new Error("Invalid payload for Completion message.")]
    ] as [number[], Error][]).forEach(([payload, expected_error]) =>
        it("throws for invalid messages", () => {
            expect(() => new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer))
                .toThrow(expected_error);
        }));

    it("can read multiple messages", () => {
        let payload = [
            0x07, 0x93, 0x02, 0xa3, 0x61, 0x62, 0x63, 0x08,
            0x0a, 0x94, 0x03, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b];
        let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
        expect(messages).toEqual([
            {
                type: MessageType.StreamItem,
                invocationId: "abc",
                item: 8
            } as ResultMessage,
            {
                type: MessageType.Completion,
                invocationId: "abc",
                error: null,
                result: "OK"
            } as CompletionMessage
        ]);
    });

    it("can read ping message", () => {
        let payload = [
            0x02,
            0x91, // message array length = 1 (fixarray)
            0x06, // type = 6 = Ping (fixnum)
        ];
        let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
        expect(messages).toEqual([
            {
                type: MessageType.Ping,
            }
        ])
    })
});