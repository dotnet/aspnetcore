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

export interface HubMessage {
    readonly type: MessageType;
}

export interface InvocationMessage extends HubMessage {
    readonly invocationId?: string;
    readonly target: string;
    readonly arguments: Array<any>;
}

export interface StreamInvocationMessage extends HubMessage {
    readonly invocationId: string;
    readonly target: string;
    readonly arguments: Array<any>
}

export interface ResultMessage extends HubMessage {
    readonly invocationId: string;
    readonly item?: any;
}

export interface CompletionMessage extends HubMessage {
    readonly invocationId: string;
    readonly error?: string;
    readonly result?: any;
}

export interface NegotiationMessage {
    readonly protocol: string;
}

export interface CancelInvocation extends HubMessage {
    readonly invocationId: string;
}

export const enum ProtocolType {
    Text = 1,
    Binary
}

export interface IHubProtocol {
    readonly name: string;
    readonly type: ProtocolType;
    parseMessages(input: any): HubMessage[];
    writeMessage(message: HubMessage): any;
}
