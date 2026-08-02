// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AckMessage, CloseMessage, CompletionMessage, InvocationMessage, MessageType, NullLogger, SequenceMessage, StreamItemMessage } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "../src/MessagePackHubProtocol";

describe("MessagePackHubProtocol", () => {
    it("can write/read non-blocking Invocation message", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {},
            streamIds: [],
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), NullLogger.instance);
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can read Invocation message with Date argument", () => {
        const invocation = {
            arguments: [new Date(Date.UTC(2018, 1, 1, 12, 34, 56))],
            headers: {},
            streamIds: [],
            target: "mymethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), NullLogger.instance);
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message with headers", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {
                foo: "bar",
            },
            streamIds: [],
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), NullLogger.instance);
        expect(parsedMessages).toEqual([invocation]);
    });

    it("can write/read Invocation message", () => {
        const invocation = {
            arguments: [42, true, "test", ["x1", "y2"], null],
            headers: {},
            invocationId: "123",
            streamIds: [],
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), NullLogger.instance);
        expect(parsedMessages).toEqual([invocation]);
    });

    ([
        [[0x0c, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x01, 0xa3, 0x45, 0x72, 0x72],
        {
            error: "Err",
            headers: {},
            invocationId: "abc",
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b],
        {
            headers: {},
            invocationId: "abc",
            result: "OK",
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x08, 0x94, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x02],
        {
            headers: {},
            invocationId: "abc",
            type: MessageType.Completion,
        } as CompletionMessage],
        [[0x0E, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xD6, 0xFF, 0x5A, 0x4A, 0x1A, 0x50],
        {
            headers: {},
            invocationId: "abc",
            result: new Date(Date.UTC(2018, 0, 1, 11, 24, 0)),
            type: MessageType.Completion,
        } as CompletionMessage],
        // extra property at the end should be ignored (testing older protocol client working with newer protocol server)
        [[0x09, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x02, 0x00],
        {
            headers: {},
            invocationId: "abc",
            type: MessageType.Completion,
        } as CompletionMessage],
    ] as [number[], CompletionMessage][]).forEach(([payload, expectedMessage]) =>
        it("can read Completion message", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance);
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
    ] as [number[], StreamItemMessage][]).forEach(([payload, expectedMessage]) =>
        it("can read StreamItem message", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance);
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
    ] as [number[], StreamItemMessage][]).forEach(([payload, expectedMessage]) =>
        it("can read message with headers", () => {
            const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance);
            expect(messages).toEqual([expectedMessage]);
        }));

    ([
        ["message with no payload", [0x00], "Invalid payload."],
        ["message with empty array", [0x01, 0x90], "Invalid payload."],
        ["message without outer array", [0x01, 0xc2], "Invalid payload."],
        ["message with invalid headers", [0x03, 0x92, 0x01, 0x05], "Invalid headers."],
        ["Invocation message with invalid invocation id", [0x03, 0x92, 0x01, 0x80], "Invalid payload for Invocation message."],
        ["StreamItem message with invalid invocation id", [0x03, 0x92, 0x02, 0x80], "Invalid payload for StreamItem message."],
        ["Completion message with invalid invocation id", [0x04, 0x93, 0x03, 0x80, 0xa0], "Invalid payload for Completion message."],
        ["Completion message with missing result", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x01], "Invalid payload for Completion message."],
        ["Completion message with missing error", [0x05, 0x94, 0x03, 0x80, 0xa0, 0x03], "Invalid payload for Completion message."],
    ] as [string, number[], string][]).forEach(([name, payload, expectedError]) =>
        it("throws for " + name, () => {
            expect(() => new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance))
                .toThrow(expectedError);
        }));

    it("can read multiple messages", () => {
        const payload = [
            0x08, 0x94, 0x02, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x08,
            0x0b, 0x95, 0x03, 0x80, 0xa3, 0x61, 0x62, 0x63, 0x03, 0xa2, 0x4f, 0x4b];
        const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance);
        expect(messages).toEqual([
            {
                headers: {},
                invocationId: "abc",
                item: 8,
                type: MessageType.StreamItem,
            } as StreamItemMessage,
            {
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
        const messages = new MessagePackHubProtocol().parseMessages(new Uint8Array(payload).buffer, NullLogger.instance);
        expect(messages).toEqual([
            {
                type: MessageType.Ping,
            },
        ]);
    });

    it("can write ping message", () => {
        const payload = new Uint8Array([
            0x02, // length prefix
            0x91, // message array length = 1 (fixarray)
            0x06, // type = 6 = Ping (fixnum)
        ]);
        const buffer = new MessagePackHubProtocol().writeMessage({ type: MessageType.Ping });
        expect(new Uint8Array(buffer)).toEqual(payload);
    });

    it("can write cancel message", () => {
        const payload = new Uint8Array([
            0x07, // length prefix
            0x93, // message array length = 1 (fixarray)
            0x05, // type = 5 = CancelInvocation (fixnum)
            0x80, // headers
            0xa3, // invocationID = string length 3
            0x61, // a
            0x62, // b
            0x63, // c
        ]);
        const buffer = new MessagePackHubProtocol().writeMessage({ type: MessageType.CancelInvocation, invocationId: "abc" });
        expect(new Uint8Array(buffer)).toEqual(payload);
    });

    it("can write completion message with false result", () => {
        const payload = new Uint8Array([
            0x09, // length prefix
            0x95, // message array length = 5 (fixarray)
            0x03, // type = 3 = Completion
            0x80, // headers
            0xa3, // invocationID = string length 3
            0x61, // a
            0x62, // b
            0x63, // c
            0x03, // result type, 3 - non-void result
            0xc2, // false
        ]);
        const buffer = new MessagePackHubProtocol().writeMessage({ type: MessageType.Completion, invocationId: "abc", result: false });
        expect(new Uint8Array(buffer)).toEqual(payload);
    });

    it("can write completion message with null result", () => {
        const payload = new Uint8Array([
            0x09, // length prefix
            0x95, // message array length = 5 (fixarray)
            0x03, // type = 3 = Completion
            0x80, // headers
            0xa3, // invocationID = string length 3
            0x61, // a
            0x62, // b
            0x63, // c
            0x03, // result type, 3 - non-void result
            0xc0, // null
        ]);
        const buffer = new MessagePackHubProtocol().writeMessage({ type: MessageType.Completion, invocationId: "abc", result: null });
        expect(new Uint8Array(buffer)).toEqual(payload);
    });

    it("will preserve double precision", () => {
        const invocation = {
            arguments: [Number(0.005)],
            headers: {},
            invocationId: "123",
            streamIds: [],
            target: "myMethod",
            type: MessageType.Invocation,
        } as InvocationMessage;

        const protocol = new MessagePackHubProtocol({ });
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(invocation), NullLogger.instance);
        expect(parsedMessages[0]).toEqual({
            arguments: [0.005],
            headers: {},
            invocationId: "123",
            streamIds: [],
            target: "myMethod",
            type: 1,
        });
    });

    it("can write/read Close message", () => {
        const closeMessage = {
            type: MessageType.Close,
        } as CloseMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(closeMessage), NullLogger.instance);
        expect(parsedMessages.length).toEqual(1);
        expect(parsedMessages[0].type).toEqual(MessageType.Close);
    });

    it("can write/read Ack message", () => {
        const ackMessage = {
            sequenceId: 13,
            type: MessageType.Ack,
        } as AckMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(ackMessage), NullLogger.instance);
        expect(parsedMessages.length).toEqual(1);
        expect(parsedMessages[0].type).toEqual(MessageType.Ack);
        expect(parsedMessages[0]).toEqual({
            sequenceId: 13,
            type: MessageType.Ack
        });
    });

    it("can write/read Sequence message", () => {
        const sequenceMessage = {
            sequenceId: 24,
            type: MessageType.Sequence,
        } as SequenceMessage;

        const protocol = new MessagePackHubProtocol();
        const parsedMessages = protocol.parseMessages(protocol.writeMessage(sequenceMessage), NullLogger.instance);
        expect(parsedMessages.length).toEqual(1);
        expect(parsedMessages[0]).toEqual({
            sequenceId: 24,
            type: MessageType.Sequence
        });
    });
});
