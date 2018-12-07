// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { Buffer } from "buffer";
import * as msgpack5 from "msgpack5";

import { CompletionMessage, HubMessage, IHubProtocol, ILogger, InvocationMessage, LogLevel, MessageHeaders, MessageType, NullLogger, StreamCompleteMessage, StreamDataMessage, StreamInvocationMessage, StreamItemMessage, TransferFormat } from "@aspnet/signalr";

import { BinaryMessageFormat } from "./BinaryMessageFormat";
import { isArrayBuffer } from "./Utils";

// TypeDoc's @inheritDoc and @link don't work across modules :(

// constant encoding of the ping message
// see: https://github.com/aspnet/SignalR/blob/dev/specs/HubProtocol.md#ping-message-encoding-1
// Don't use Uint8Array.from as IE does not support it
const SERIALIZED_PING_MESSAGE: Uint8Array = new Uint8Array([0x91, MessageType.Ping]);

/** Implements the MessagePack Hub Protocol */
export class MessagePackHubProtocol implements IHubProtocol {
    /** The name of the protocol. This is used by SignalR to resolve the protocol between the client and server. */
    public readonly name: string = "messagepack";
    /** The version of the protocol. */
    public readonly version: number = 1;
    /** The TransferFormat of the protocol. */
    public readonly transferFormat: TransferFormat = TransferFormat.Binary;

    /** Creates an array of HubMessage objects from the specified serialized representation.
     *
     * @param {ArrayBuffer | Buffer} input An ArrayBuffer containing the serialized representation.
     * @param {ILogger} logger A logger that will be used to log messages that occur during parsing.
     */
    public parseMessages(input: ArrayBuffer | Buffer, logger: ILogger): HubMessage[] {
        // The interface does allow "string" to be passed in, but this implementation does not. So let's throw a useful error.
        if (!(input instanceof Buffer) && !(isArrayBuffer(input))) {
            throw new Error("Invalid input for MessagePack hub protocol. Expected an ArrayBuffer or Buffer.");
        }

        if (logger === null) {
            logger = NullLogger.instance;
        }

        const messages = BinaryMessageFormat.parse(input);

        const hubMessages = [];
        for (const message of messages) {
            const parsedMessage = this.parseMessage(message, logger);
            // Can be null for an unknown message. Unknown message is logged in parseMessage
            if (parsedMessage) {
                hubMessages.push(parsedMessage);
            }
        }

        return hubMessages;
    }

    /** Writes the specified HubMessage to an ArrayBuffer and returns it.
     *
     * @param {HubMessage} message The message to write.
     * @returns {ArrayBuffer} An ArrayBuffer containing the serialized representation of the message.
     */
    public writeMessage(message: HubMessage): ArrayBuffer {
        switch (message.type) {
            case MessageType.Invocation:
                return this.writeInvocation(message as InvocationMessage);
            case MessageType.StreamInvocation:
                return this.writeStreamInvocation(message as StreamInvocationMessage);
            case MessageType.StreamData:
                return this.writeStreamData(message as StreamDataMessage);
            case MessageType.StreamItem:
            case MessageType.Completion:
                throw new Error(`Writing messages of type '${message.type}' is not supported.`);
            case MessageType.Ping:
                return BinaryMessageFormat.write(SERIALIZED_PING_MESSAGE);
            case MessageType.StreamComplete:
                return this.writeStreamComplete(message as StreamCompleteMessage);
            default:
                throw new Error("Invalid message type.");
        }
    }

    private parseMessage(input: Uint8Array, logger: ILogger): HubMessage | null {
        if (input.length === 0) {
            throw new Error("Invalid payload.");
        }

        const msgpack = msgpack5();
        const properties = msgpack.decode(Buffer.from(input));
        if (properties.length === 0 || !(properties instanceof Array)) {
            throw new Error("Invalid payload.");
        }

        const messageType = properties[0] as MessageType;

        switch (messageType) {
            case MessageType.Invocation:
                return this.createInvocationMessage(this.readHeaders(properties), properties);
            case MessageType.StreamItem:
                return this.createStreamItemMessage(this.readHeaders(properties), properties);
            case MessageType.Completion:
                return this.createCompletionMessage(this.readHeaders(properties), properties);
            case MessageType.Ping:
                return this.createPingMessage(properties);
            case MessageType.Close:
                return this.createCloseMessage(properties);
            default:
                // Future protocol changes can add message types, old clients can ignore them
                logger.log(LogLevel.Information, "Unknown message type '" + messageType + "' ignored.");
                return null;
        }
    }

    private createCloseMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 2) {
            throw new Error("Invalid payload for Close message.");
        }

        return {
            // Close messages have no headers.
            error: properties[1],
            type: MessageType.Close,
        } as HubMessage;
    }

    private createPingMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 1) {
            throw new Error("Invalid payload for Ping message.");
        }

        return {
            // Ping messages have no headers.
            type: MessageType.Ping,
        } as HubMessage;
    }

    private createInvocationMessage(headers: MessageHeaders, properties: any[]): InvocationMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 5) {
            throw new Error("Invalid payload for Invocation message.");
        }

        const invocationId = properties[2] as string;
        if (invocationId) {
            return {
                arguments: properties[4],
                headers,
                invocationId,
                target: properties[3] as string,
                type: MessageType.Invocation,
            };
        } else {
            return {
                arguments: properties[4],
                headers,
                target: properties[3],
                type: MessageType.Invocation,
            };
        }

    }

    private createStreamItemMessage(headers: MessageHeaders, properties: any[]): StreamItemMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 4) {
            throw new Error("Invalid payload for StreamItem message.");
        }

        return {
            headers,
            invocationId: properties[2],
            item: properties[3],
            type: MessageType.StreamItem,
        } as StreamItemMessage;
    }

    private createCompletionMessage(headers: MessageHeaders, properties: any[]): CompletionMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 4) {
            throw new Error("Invalid payload for Completion message.");
        }

        const errorResult = 1;
        const voidResult = 2;
        const nonVoidResult = 3;

        const resultKind = properties[3];

        if (resultKind !== voidResult && properties.length < 5) {
            throw new Error("Invalid payload for Completion message.");
        }

        let error: string | undefined;
        let result: any;

        switch (resultKind) {
            case errorResult:
                error = properties[4];
                break;
            case nonVoidResult:
                result = properties[4];
                break;
        }

        const completionMessage: CompletionMessage = {
            error,
            headers,
            invocationId: properties[2],
            result,
            type: MessageType.Completion,
        };

        return completionMessage;
    }

    private writeInvocation(invocationMessage: InvocationMessage): ArrayBuffer {
        const msgpack = msgpack5();
        const payload = msgpack.encode([MessageType.Invocation, invocationMessage.headers || {}, invocationMessage.invocationId || null,
        invocationMessage.target, invocationMessage.arguments]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private writeStreamInvocation(streamInvocationMessage: StreamInvocationMessage): ArrayBuffer {
        const msgpack = msgpack5();
        const payload = msgpack.encode([MessageType.StreamInvocation, streamInvocationMessage.headers || {}, streamInvocationMessage.invocationId,
        streamInvocationMessage.target, streamInvocationMessage.arguments]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private writeStreamData(streamDataMessage: StreamDataMessage): ArrayBuffer {
        const msgpack = msgpack5();
        const payload = msgpack.encode([MessageType.StreamData, streamDataMessage.streamId,
            streamDataMessage.item]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private writeStreamComplete(streamCompleteMessage: StreamCompleteMessage): ArrayBuffer {
        const msgpack = msgpack5();
        const payload = msgpack.encode([MessageType.StreamComplete, streamCompleteMessage.streamId,
            streamCompleteMessage.error || null]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private readHeaders(properties: any): MessageHeaders {
        const headers: MessageHeaders = properties[1] as MessageHeaders;
        if (typeof headers !== "object") {
            throw new Error("Invalid headers.");
        }
        return headers;
    }
}
