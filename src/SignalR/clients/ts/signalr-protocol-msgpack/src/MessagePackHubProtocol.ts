// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Encoder, Decoder } from "@msgpack/msgpack";

import { MessagePackOptions } from "./MessagePackOptions";

import {
    AckMessage,
    CancelInvocationMessage, CompletionMessage, HubMessage, IHubProtocol, ILogger, InvocationMessage,
    LogLevel, MessageHeaders, MessageType, NullLogger, SequenceMessage, StreamInvocationMessage, StreamItemMessage, TransferFormat,
} from "@microsoft/signalr";

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
    public readonly version: number = 2;
    /** The TransferFormat of the protocol. */
    public readonly transferFormat: TransferFormat = TransferFormat.Binary;

    private readonly _errorResult = 1;
    private readonly _voidResult = 2;
    private readonly _nonVoidResult = 3;

    private readonly _encoder: Encoder<undefined>;
    private readonly _decoder: Decoder<undefined>;

    /**
     *
     * @param messagePackOptions MessagePack options passed to @msgpack/msgpack
     */
    constructor(messagePackOptions?: MessagePackOptions) {
        messagePackOptions = messagePackOptions || {};
        this._encoder = new Encoder(
            messagePackOptions.extensionCodec,
            messagePackOptions.context,
            messagePackOptions.maxDepth,
            messagePackOptions.initialBufferSize,
            messagePackOptions.sortKeys,
            messagePackOptions.forceFloat32,
            messagePackOptions.ignoreUndefined,
            messagePackOptions.forceIntegerToFloat,
        );

        this._decoder = new Decoder(
            messagePackOptions.extensionCodec,
            messagePackOptions.context,
            messagePackOptions.maxStrLength,
            messagePackOptions.maxBinLength,
            messagePackOptions.maxArrayLength,
            messagePackOptions.maxMapLength,
            messagePackOptions.maxExtLength,
        );
    }

    /** Creates an array of HubMessage objects from the specified serialized representation.
     *
     * @param {ArrayBuffer} input An ArrayBuffer containing the serialized representation.
     * @param {ILogger} logger A logger that will be used to log messages that occur during parsing.
     */
    public parseMessages(input: ArrayBuffer, logger: ILogger): HubMessage[] {
        // The interface does allow "string" to be passed in, but this implementation does not. So let's throw a useful error.
        if (!(isArrayBuffer(input))) {
            throw new Error("Invalid input for MessagePack hub protocol. Expected an ArrayBuffer.");
        }

        if (logger === null) {
            logger = NullLogger.instance;
        }

        const messages = BinaryMessageFormat.parse(input);

        const hubMessages = [];
        for (const message of messages) {
            const parsedMessage = this._parseMessage(message, logger);
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
                return this._writeInvocation(message as InvocationMessage);
            case MessageType.StreamInvocation:
                return this._writeStreamInvocation(message as StreamInvocationMessage);
            case MessageType.StreamItem:
                return this._writeStreamItem(message as StreamItemMessage);
            case MessageType.Completion:
                return this._writeCompletion(message as CompletionMessage);
            case MessageType.Ping:
                return BinaryMessageFormat.write(SERIALIZED_PING_MESSAGE);
            case MessageType.CancelInvocation:
                return this._writeCancelInvocation(message as CancelInvocationMessage);
            case MessageType.Close:
                return this._writeClose();
            case MessageType.Ack:
                return this._writeAck(message as AckMessage);
            case MessageType.Sequence:
                return this._writeSequence(message as SequenceMessage);
            default:
                throw new Error("Invalid message type.");
        }
    }

    private _parseMessage(input: Uint8Array, logger: ILogger): HubMessage | null {
        if (input.length === 0) {
            throw new Error("Invalid payload.");
        }

        const properties = this._decoder.decode(input) as any;
        if (properties.length === 0 || !(properties instanceof Array)) {
            throw new Error("Invalid payload.");
        }

        const messageType = properties[0] as MessageType;

        switch (messageType) {
            case MessageType.Invocation:
                return this._createInvocationMessage(this._readHeaders(properties), properties);
            case MessageType.StreamItem:
                return this._createStreamItemMessage(this._readHeaders(properties), properties);
            case MessageType.Completion:
                return this._createCompletionMessage(this._readHeaders(properties), properties);
            case MessageType.Ping:
                return this._createPingMessage(properties);
            case MessageType.Close:
                return this._createCloseMessage(properties);
            case MessageType.Ack:
                return this._createAckMessage(properties);
            case MessageType.Sequence:
                return this._createSequenceMessage(properties);
            default:
                // Future protocol changes can add message types, old clients can ignore them
                logger.log(LogLevel.Information, "Unknown message type '" + messageType + "' ignored.");
                return null;
        }
    }

    private _createCloseMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 2) {
            throw new Error("Invalid payload for Close message.");
        }

        return {
            // Close messages have no headers.
            allowReconnect: properties.length >= 3 ? properties[2] : undefined,
            error: properties[1],
            type: MessageType.Close,
        } as HubMessage;
    }

    private _createPingMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 1) {
            throw new Error("Invalid payload for Ping message.");
        }

        return {
            // Ping messages have no headers.
            type: MessageType.Ping,
        } as HubMessage;
    }

    private _createInvocationMessage(headers: MessageHeaders, properties: any[]): InvocationMessage {
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
                streamIds: [],
                target: properties[3] as string,
                type: MessageType.Invocation,
            };
        } else {
            return {
                arguments: properties[4],
                headers,
                streamIds: [],
                target: properties[3],
                type: MessageType.Invocation,
            };
        }

    }

    private _createStreamItemMessage(headers: MessageHeaders, properties: any[]): StreamItemMessage {
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

    private _createCompletionMessage(headers: MessageHeaders, properties: any[]): CompletionMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 4) {
            throw new Error("Invalid payload for Completion message.");
        }

        const resultKind = properties[3];

        if (resultKind !== this._voidResult && properties.length < 5) {
            throw new Error("Invalid payload for Completion message.");
        }

        let error: string | undefined;
        let result: any;

        switch (resultKind) {
            case this._errorResult:
                error = properties[4];
                break;
            case this._nonVoidResult:
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

    private _createAckMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 1) {
            throw new Error("Invalid payload for Ack message.");
        }

        return {
            sequenceId: properties[1],
            type: MessageType.Ack,
        } as HubMessage;
    }

    private _createSequenceMessage(properties: any[]): HubMessage {
        // check minimum length to allow protocol to add items to the end of objects in future releases
        if (properties.length < 1) {
            throw new Error("Invalid payload for Sequence message.");
        }

        return {
            sequenceId: properties[1],
            type: MessageType.Sequence,
        } as HubMessage;
    }

    private _writeInvocation(invocationMessage: InvocationMessage): ArrayBuffer {
        let payload: any;
        if (invocationMessage.streamIds) {
            payload = this._encoder.encode([MessageType.Invocation, invocationMessage.headers || {}, invocationMessage.invocationId || null,
            invocationMessage.target, invocationMessage.arguments, invocationMessage.streamIds]);
        } else {
            payload = this._encoder.encode([MessageType.Invocation, invocationMessage.headers || {}, invocationMessage.invocationId || null,
            invocationMessage.target, invocationMessage.arguments]);
        }

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeStreamInvocation(streamInvocationMessage: StreamInvocationMessage): ArrayBuffer {
        let payload: any;
        if (streamInvocationMessage.streamIds) {
            payload = this._encoder.encode([MessageType.StreamInvocation, streamInvocationMessage.headers || {}, streamInvocationMessage.invocationId,
            streamInvocationMessage.target, streamInvocationMessage.arguments, streamInvocationMessage.streamIds]);
        } else {
            payload = this._encoder.encode([MessageType.StreamInvocation, streamInvocationMessage.headers || {}, streamInvocationMessage.invocationId,
            streamInvocationMessage.target, streamInvocationMessage.arguments]);
        }

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeStreamItem(streamItemMessage: StreamItemMessage): ArrayBuffer {
        const payload = this._encoder.encode([MessageType.StreamItem, streamItemMessage.headers || {}, streamItemMessage.invocationId,
        streamItemMessage.item]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeCompletion(completionMessage: CompletionMessage): ArrayBuffer {
        const resultKind = completionMessage.error ? this._errorResult :
            (completionMessage.result !== undefined) ? this._nonVoidResult : this._voidResult;

        let payload: any;
        switch (resultKind) {
            case this._errorResult:
                payload = this._encoder.encode([MessageType.Completion, completionMessage.headers || {}, completionMessage.invocationId, resultKind, completionMessage.error]);
                break;
            case this._voidResult:
                payload = this._encoder.encode([MessageType.Completion, completionMessage.headers || {}, completionMessage.invocationId, resultKind]);
                break;
            case this._nonVoidResult:
                payload = this._encoder.encode([MessageType.Completion, completionMessage.headers || {}, completionMessage.invocationId, resultKind, completionMessage.result]);
                break;
        }

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeCancelInvocation(cancelInvocationMessage: CancelInvocationMessage): ArrayBuffer {
        const payload = this._encoder.encode([MessageType.CancelInvocation, cancelInvocationMessage.headers || {}, cancelInvocationMessage.invocationId]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeClose(): ArrayBuffer {
        const payload = this._encoder.encode([MessageType.Close, null]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeAck(ackMessage: AckMessage): ArrayBuffer {
        const payload = this._encoder.encode([MessageType.Ack, ackMessage.sequenceId]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private _writeSequence(sequenceMessage: SequenceMessage): ArrayBuffer {
        const payload = this._encoder.encode([MessageType.Sequence, sequenceMessage.sequenceId]);

        return BinaryMessageFormat.write(payload.slice());
    }

    private _readHeaders(properties: any): MessageHeaders {
        const headers: MessageHeaders = properties[1] as MessageHeaders;
        if (typeof headers !== "object") {
            throw new Error("Invalid headers.");
        }
        return headers;
    }
}
