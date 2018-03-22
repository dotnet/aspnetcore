// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { CompletionMessage, InvocationMessage, MessageType, NullLogger, StreamItemMessage } from "@aspnet/signalr";
import { MessagePackHubProtocol } from "../src/MessagePackHubProtocol";

describe("MessageHubProtocol", () => {
    it("can write/read non-blocking Invocation message", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {},
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), new NullLogger());
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can read Invocation message with Date argument", () => {
        const invocation = {
            arguments: [new Date(Date.UTC(2018, 1, 1, 12, 34, 56))],
            headers: {},
            target: "mymethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), new NullLogger());
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message with headers", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {
                foo: "bar",
            },
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), new NullLogger());
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {},
            invocationId: "123",
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), new NullLogger());
        expect(parsedMessages).toEqual([invocation]);
    });

    ([
        [[0x0c, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x01, 0xa3, 0x45, 0x72, 0x72],
        {
            error: "Err",
            headers: {},
            invocationId: "abc",
            result: null,
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b],
        {
            error: null,
            headers: {},
            invocationId: "abc",
            result: "OK",
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x08, 0x94, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x02],
        {
            error: null,
            headers: {},
            invocationId: "abc",
            result: null,
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x0E, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xD6, 0xFF, 0x5A, 0x4A, 0x1A, 0x50],
        {
            error: null,
            headers: {},
            invocationId: "abc",
            result: new Date(Date.UTC(2018, 0, 1, 11, 24, 0)),
            type: MessageType.Completion,
        } as CompletionMessage],
        // extra property at the end should be ignored (testing older protocol client working with newer protocol server)
        [[0x09, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x02, 0x00],
        {
            error: null,
            headers: {},
            invocationId: "abc",
            result: null,
            type: MessageType.Completion,
        } as CompletionMessage],
    ] as Array<[number[], CompletionMessage]>).forEach(([payload, expectedMessage]) =>
        it("can read Completion message", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger());
            expect(messages).toEqual([expectedMessage]);
        }));

    ([
        [[0x08, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x08],
        {
            headers: {},
            invocationId: "abc",
            item: 8,
            type: MessageType.StreamItem,
        } as StreamItemMessage],
        [[0x0D, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0xD6, 0xFF, 0x5A, 0x4A, 0x1A, 0x50],
        {
            headers: {},
            invocationId: "abc",
            item: new Date(Date.UTC(2018, 0, 1, 11, 24, 0)),
            type: MessageType.StreamItem,
        } as StreamItemMessage],
    ] as Array<[number[], StreamItemMessage]>).forEach(([payload, expectedMessage]) =>
        it("can read StreamItem message", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger());
            expect(messages).toEqual([expectedMessage]);
        }));

    ([
        [[0x0c, 0x94, 0x02, 0x81, 0xa1, 0x74, 0xa1, 0x75, 0xa3, 0x61, 0x62, 0x63, 0x08],
        {
            headers: {
                t: "u",
            },
            invocationId: "abc",
            item: 8,
            type: MessageType.StreamItem,
        } as StreamItemMessage],
    ] as Array<[number[], StreamItemMessage]>).forEach(([payload, expectedMessage]) =>
        it("can read message with headers", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger());
            expect(messages).toEqual([expectedMessage]);
        }));

    ([
        ["message with no payload", [0x00], new Error("Invalid payload.")],
        ["message with empty array", [0x01, 0x90], new Error("Invalid payload.")],
        ["message without outer array", [0x01, 0xc2], new Error("Invalid payload.")],
        ["message with invalid headers", [0x03, 0x92, 0x01, 0x05], new Error("Invalid headers.")],
        ["Invocation message with invalid invocation id", [0x03, 0x92, 0x01, 0x80], new Error("Invalid payload for Invocation message.")],
        ["StreamItem message with invalid invocation id", [0x03, 0x92, 0x02, 0x80], new Error("Invalid payload for StreamItem message.")],
        ["Completion message with invalid invocation id", [0x04, 0x93, 0x03, 0x80, 0xa0], new Error("Invalid payload for Completion message.")],
        ["Completion message with missing result", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x01], new Error("Invalid payload for Completion message.")],
        ["Completion message with missing error", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x03], new Error("Invalid payload for Completion message.")],
    ] as Array<[string, number[], Error]>).forEach(([name, payload, expectedError]) =>
        it("throws for " + name, () => {
            expect(() => new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger()))
                .toThrow(expectedError);
        }));

    it("can read multiple messages", () => {
        const payload = [
            0x08, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x08,
            0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b];
        const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger());
        expect(messages).toEqual([
            {
                headers: {},
                invocationId: "abc",
                item: 8,
                type: MessageType.StreamItem,
            } as StreamItemMessage,
            {
                error: null,
                headers: {},
                invocationId: "abc",
                result: "OK",
                type: MessageType.Completion,
            } as CompletionMessage,
        ]);
    });

    it("can read ping message", () => {
        const payload = [
            0x02,
            0x91, // message array length = 1 (fixarray)
            0x06, // type = 6 = Ping (fixnum)
        ];
        const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, new NullLogger());
        expect(messages).toEqual([
            {
                type: MessageType.Ping,
            },
        ]);
    });
});
