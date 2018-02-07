// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export const enum MessageType {
    Invocation = 1,
    StreamItem = 2,
    Completion = 3,
    StreamInvocation = 4,
    CancelInvocation = 5,
    Ping = 6,
}

export interface MessageHeaders { [key: string]: string; }

export type HubMessage = InvocationMessage | StreamInvocationMessage | StreamItemMessage | CompletionMessage | CancelInvocationMessage | PingMessage;

export interface HubMessageBase {
    readonly type: MessageType;
}

export interface HubInvocationMessage extends HubMessageBase {
    readonly headers?: MessageHeaders;
    readonly invocationId?: string;
}

export interface InvocationMessage extends HubInvocationMessage {
    readonly type: MessageType.Invocation;
    readonly target: string;
    readonly arguments: any[];
}

export interface StreamInvocationMessage extends HubInvocationMessage {
    readonly type: MessageType.StreamInvocation;
    readonly target: string;
    readonly arguments: any[];
}

export interface StreamItemMessage extends HubInvocationMessage {
    readonly type: MessageType.StreamItem;
    readonly item?: any;
}

export interface CompletionMessage extends HubInvocationMessage {
    readonly type: MessageType.Completion;
    readonly error?: string;
    readonly result?: any;
}

export interface NegotiationMessage {
    readonly protocol: string;
}

export interface PingMessage extends HubInvocationMessage {
    readonly type: MessageType.Ping;
}

export interface CancelInvocationMessage extends HubInvocationMessage {
    readonly type: MessageType.CancelInvocation;
}

export const enum ProtocolType {
    Text = 1,
    Binary,
}

export interface IHubProtocol {
    readonly name: string;
    readonly type: ProtocolType;
    parseMessages(input: any): HubMessage[];
    writeMessage(message: HubMessage): any;
}
