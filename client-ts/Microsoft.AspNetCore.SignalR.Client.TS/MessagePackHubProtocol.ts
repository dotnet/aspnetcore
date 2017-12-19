// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IHubProtocol, ProtocolType, MessageType, HubMessage, InvocationMessage, ResultMessage, CompletionMessage, StreamInvocationMessage } from "./IHubProtocol";
import { BinaryMessageFormat } from "./Formatters"
import * as msgpack5 from "msgpack5"

export class MessagePackHubProtocol implements IHubProtocol {

    readonly name: string = "messagepack";

    readonly type: ProtocolType = ProtocolType.Binary;

    parseMessages(input: ArrayBuffer): HubMessage[] {
        return BinaryMessageFormat.parse(input).map(m => this.parseMessage(m));
    }

    private parseMessage(input: Uint8Array): HubMessage {
        if (input.length == 0) {
            throw new Error("Invalid payload.");
        }

        let msgpack = msgpack5();
        let properties = msgpack.decode(new Buffer(input));
        if (properties.length == 0 || !(properties instanceof Array)) {
            throw new Error("Invalid payload.");
        }

        let messageType = properties[0] as MessageType;
        switch (messageType) {
            case MessageType.Invocation:
                return this.createInvocationMessage(properties);
            case MessageType.StreamItem:
                return this.createStreamItemMessage(properties);
            case MessageType.Completion:
                return this.createCompletionMessage(properties);
            case MessageType.Ping:
                return this.createPingMessage(properties);
            default:
                throw new Error("Invalid message type.");
        }
    }

    private createPingMessage(properties: any[]): HubMessage {
        if (properties.length != 1) {
            throw new Error("Invalid payload for Ping message.");
        }

        return {
            type: properties[0]
        } as HubMessage;
    }

    private createInvocationMessage(properties: any[]): InvocationMessage {
        if (properties.length != 4) {
            throw new Error("Invalid payload for Invocation message.");
        }

        let invocationId = properties[1];
        if (invocationId) {
            return {
                type: MessageType.Invocation,
                invocationId: invocationId,
                target: properties[2],
                arguments: properties[3]
            } as InvocationMessage;
        }
        else {
            return {
                type: MessageType.Invocation,
                target: properties[2],
                arguments: properties[3]
            } as InvocationMessage;
        }

    }

    private createStreamItemMessage(properties: any[]): ResultMessage {
        if (properties.length != 3) {
            throw new Error("Invalid payload for stream Result message.");
        }

        return {
            type: MessageType.StreamItem,
            invocationId: properties[1],
            item: properties[2]
        } as ResultMessage;
    }

    private createCompletionMessage(properties: any[]): CompletionMessage {
        if (properties.length < 3) {
            throw new Error("Invalid payload for Completion message.");
        }

        const errorResult = 1;
        const voidResult = 2;
        const nonVoidResult = 3;

        let resultKind = properties[2];

        if ((resultKind === voidResult && properties.length != 3) ||
            (resultKind !== voidResult && properties.length != 4)) {
            throw new Error("Invalid payload for Completion message.");
        }

        let completionMessage = {
            type: MessageType.Completion,
            invocationId: properties[1],
            error: null as string,
            result: null as any
        };

        switch (resultKind) {
            case errorResult:
                completionMessage.error = properties[3];
                break;
            case nonVoidResult:
                completionMessage.result = properties[3];
                break;
        }

        return completionMessage as CompletionMessage;
    }

    writeMessage(message: HubMessage): ArrayBuffer {
        switch (message.type) {
            case MessageType.Invocation:
                return this.writeInvocation(message as InvocationMessage);
            case MessageType.StreamInvocation:
                return this.writeStreamInvocation(message as StreamInvocationMessage);
            case MessageType.StreamItem:
            case MessageType.Completion:
                throw new Error(`Writing messages of type '${message.type}' is not supported.`);
            default:
                throw new Error("Invalid message type.");
        }
    }

    private writeInvocation(invocationMessage: InvocationMessage): ArrayBuffer {
        let msgpack = msgpack5();
        let payload = msgpack.encode([MessageType.Invocation, invocationMessage.invocationId || null,
        invocationMessage.target, invocationMessage.arguments]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private writeStreamInvocation(streamInvocationMessage: StreamInvocationMessage): ArrayBuffer {
        let msgpack = msgpack5();
        let payload = msgpack.encode([MessageType.StreamInvocation, streamInvocationMessage.invocationId,
        streamInvocationMessage.target, streamInvocationMessage.arguments]);

        return BinaryMessageFormat.write(payload.slice());
    }
}
