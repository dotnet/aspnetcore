// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { MessagePackHubProtocol } from "../src/MessagePackHubProtocol"
import { MessageType, InvocationMessage, CompletionMessage, StreamItemMessage } from "@aspnet/signalr"

describe("MessageHubProtocol", () => {
    it("can write/read non-blocking Invocation message", () => {
        let invocation = <InvocationMessage>{
            headers: {},
            type: MessageType.Invocation,
            target: "myMethod",
            arguments: [42, true, "test", ["x1", "y2"], null]
        };

        let protocol = new MessagePackHubProtocol();
        var parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation));
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message with headers", () => {
        let invocation = <InvocationMessage>{
            headers: {
                "foo": "bar"
            },
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
            headers: {},
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
        [[0x0c, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x01, 0xa3, 0x45, 0x72, 0x72],
        {
            headers: {},
            type: MessageType.Completion,
            invocationId: "abc",
            error: "Err",
            result: null
        } as CompletionMessage],
        [[0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b],
        {
            headers: {},
            type: MessageType.Completion,
            invocationId: "abc",
            error: null,
            result: "OK"
        } as CompletionMessage],
        [[0x08, 0x94, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x02],
        {
            headers: {},
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
        [[0x08, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x08],
        {
            headers: {},
            type: MessageType.StreamItem,
            invocationId: "abc",
            item: 8
        } as StreamItemMessage]
    ] as [[number[], StreamItemMessage]]).forEach(([payload, expected_message]) =>
        it("can read StreamItem message", () => {
            let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
            expect(messages).toEqual([expected_message]);
        }));

    ([
        [[0x0c, 0x94, 0x02, 0x81, 0xa1, 0x74, 0xa1, 0x75, 0xa3, 0x61, 0x62, 0x63, 0x08],
        {
            headers: {
                "t": "u"
            },
            type: MessageType.StreamItem,
            invocationId: "abc",
            item: 8
        } as StreamItemMessage]
    ] as [[number[], StreamItemMessage]]).forEach(([payload, expected_message]) =>
        it("can read message with headers", () => {
            let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
            expect(messages).toEqual([expected_message]);
        }));

    ([
        ["message with no payload", [0x00], new Error("Invalid payload.")],
        ["message with empty array", [0x01, 0x90], new Error("Invalid payload.")],
        ["message without outer array", [0x01, 0xc2], new Error("Invalid payload.")],
        ["message with out-of-range message type", [0x03, 0x92, 0x05, 0x80], new Error("Invalid message type.")],
        ["message with non-integer message type", [0x04, 0x92, 0xa1, 0x78, 0x80], new Error("Invalid message type.")],
        ["message with invalid headers", [0x03, 0x92, 0x01, 0x05], new Error("Invalid headers.")],
        ["Invocation message with invalid invocation id", [0x03, 0x92, 0x01, 0x80], new Error("Invalid payload for Invocation message.")],
        ["StreamItem message with invalid invocation id", [0x03, 0x92, 0x02, 0x80], new Error("Invalid payload for stream Result message.")],
        ["Completion message with invalid invocation id", [0x04, 0x93, 0x03, 0x80, 0xa0], new Error("Invalid payload for Completion message.")],
        ["Completion message with unexpected result", [0x06, 0x95, 0x03, 0x80, 0xa0, 0x02, 0x00], new Error("Invalid payload for Completion message.")],
        ["Completion message with missing result", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x01], new Error("Invalid payload for Completion message.")],
        ["Completion message with missing error", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x03], new Error("Invalid payload for Completion message.")]
    ] as [string, number[], Error][]).forEach(([name, payload, expected_error]) =>
        it("throws for " + name, () => {
            expect(() => new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer))
                .toThrow(expected_error);
        }));

    it("can read multiple messages", () => {
        let payload = [
            0x08, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x08,
            0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b];
        let messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer);
        expect(messages).toEqual([
            {
                headers: {},
                type: MessageType.StreamItem,
                invocationId: "abc",
                item: 8
            } as StreamItemMessage,
            {
                headers: {},
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