// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger } from "./ILogger";
import { TransferFormat } from "./ITransport";

/** Defines the type of a Hub Message. */
export enum MessageType {
    /** Indicates the message is an Invocation message and implements the {@link @aspnet/signalr.InvocationMessage} interface. */
    Invocation = 1,
    /** Indicates the message is a StreamItem message and implements the {@link @aspnet/signalr.StreamItemMessage} interface. */
    StreamItem = 2,
    /** Indicates the message is a Completion message and implements the {@link @aspnet/signalr.CompletionMessage} interface. */
    Completion = 3,
    /** Indicates the message is a Stream Invocation message and implements the {@link @aspnet/signalr.StreamInvocationMessage} interface. */
    StreamInvocation = 4,
    /** Indicates the message is a Cancel Invocation message and implements the {@link @aspnet/signalr.CancelInvocationMessage} interface. */
    CancelInvocation = 5,
    /** Indicates the message is a Ping message and implements the {@link @aspnet/signalr.PingMessage} interface. */
    Ping = 6,
    /** Indicates the message is a Close message and implements the {@link @aspnet/signalr.CloseMessage} interface. */
    Close = 7,
}

/** Defines a dictionary of string keys and string values representing headers attached to a Hub message. */
export interface MessageHeaders {
    /** Gets or sets the header with the specified key. */
    [key: string]: string;
}

/** Union type of all known Hub messages. */
export type HubMessage =
    InvocationMessage |
    StreamInvocationMessage |
    StreamItemMessage |
    CompletionMessage |
    CancelInvocationMessage |
    PingMessage |
    CloseMessage;

/** Defines properties common to all Hub messages. */
export interface HubMessageBase {
    /** A {@link @aspnet/signalr.MessageType} value indicating the type of this message. */
    readonly type: MessageType;
}

/** Defines properties common to all Hub messages relating to a specific invocation. */
export interface HubInvocationMessage extends HubMessageBase {
    /** A {@link @aspnet/signalr.MessageHeaders} dictionary containing headers attached to the message. */
    readonly headers?: MessageHeaders;
    /** The ID of the invocation relating to this message.
     *
     * This is expected to be present for {@link @aspnet/signalr.StreamInvocationMessage} and {@link @aspnet/signalr.CompletionMessage}. It may
     * be 'undefined' for an {@link @aspnet/signalr.InvocationMessage} if the sender does not expect a response.
     */
    readonly invocationId?: string;
}

/** A hub message representing a non-streaming invocation. */
export interface InvocationMessage extends HubInvocationMessage {
    /** @inheritDoc */
    readonly type: MessageType.Invocation;
    /** The target method name. */
    readonly target: string;
    /** The target method arguments. */
    readonly arguments: any[];
}

/** A hub message representing a streaming invocation. */
export interface StreamInvocationMessage extends HubInvocationMessage {
    /** @inheritDoc */
    readonly type: MessageType.StreamInvocation;

    /** The invocation ID. */
    readonly invocationId: string;
    /** The target method name. */
    readonly target: string;
    /** The target method arguments. */
    readonly arguments: any[];
}

/** A hub message representing a single item produced as part of a result stream. */
export interface StreamItemMessage extends HubInvocationMessage {
    /** @inheritDoc */
    readonly type: MessageType.StreamItem;

    /** The invocation ID. */
    readonly invocationId: string;

    /** The item produced by the server. */
    readonly item?: any;
}

/** A hub message representing the result of an invocation. */
export interface CompletionMessage extends HubInvocationMessage {
    /** @inheritDoc */
    readonly type: MessageType.Completion;
    /** The invocation ID. */
    readonly invocationId: string;
    /** The error produced by the invocation, if any.
     *
     * Either {@link @aspnet/signalr.CompletionMessage.error} or {@link @aspnet/signalr.CompletionMessage.result} must be defined, but not both.
     */
    readonly error?: string;
    /** The result produced by the invocation, if any.
     *
     * Either {@link @aspnet/signalr.CompletionMessage.error} or {@link @aspnet/signalr.CompletionMessage.result} must be defined, but not both.
     */
    readonly result?: any;
}

/** A hub message indicating that the sender is still active. */
export interface PingMessage extends HubMessageBase {
    /** @inheritDoc */
    readonly type: MessageType.Ping;
}

/** A hub message indicating that the sender is closing the connection.
 *
 * If {@link @aspnet/signalr.CloseMessage.error} is defined, the sender is closing the connection due to an error.
 */
export interface CloseMessage extends HubMessageBase {
    /** @inheritDoc */
    readonly type: MessageType.Close;
    /** The error that triggered the close, if any.
     *
     * If this property is undefined, the connection was closed normally and without error.
     */
    readonly error?: string;
}

/** A hub message sent to request that a streaming invocation be canceled. */
export interface CancelInvocationMessage extends HubInvocationMessage {
    /** @inheritDoc */
    readonly type: MessageType.CancelInvocation;
    /** The invocation ID. */
    readonly invocationId: string;
}

/** A protocol abstraction for communicating with SignalR Hubs.  */
export interface IHubProtocol {
    /** The name of the protocol. This is used by SignalR to resolve the protocol between the client and server. */
    readonly name: string;
    /** The version of the protocol. */
    readonly version: number;
    /** The {@link @aspnet/signalr.TransferFormat} of the protocol. */
    readonly transferFormat: TransferFormat;

    /** Creates an array of {@link @aspnet/signalr.HubMessage} objects from the specified serialized representation.
     *
     * If {@link @aspnet/signalr.IHubProtocol.transferFormat} is 'Text', the `input` parameter must be a string, otherwise it must be an ArrayBuffer.
     *
     * @param {string | ArrayBuffer} input A string, or ArrayBuffer containing the serialized representation.
     * @param {ILogger} logger A logger that will be used to log messages that occur during parsing.
     */
    parseMessages(input: string | ArrayBuffer, logger: ILogger): HubMessage[];

    /** Writes the specified {@link @aspnet/signalr.HubMessage} to a string or ArrayBuffer and returns it.
     *
     * If {@link @aspnet/signalr.IHubProtocol.transferFormat} is 'Text', the result of this method will be a string, otherwise it will be an ArrayBuffer.
     *
     * @param {HubMessage} message The message to write.
     * @returns {string | ArrayBuffer} A string or ArrayBuffer containing the serialized representation of the message.
     */
    writeMessage(message: HubMessage): string | ArrayBuffer;
}
