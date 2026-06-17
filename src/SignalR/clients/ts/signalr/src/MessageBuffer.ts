// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IConnection } from "./IConnection";
import { AckMessage, HubMessage, IHubProtocol, MessageType, SequenceMessage } from "./IHubProtocol";
import { isArrayBuffer } from "./Utils";

/** @private */
export class MessageBuffer {
    private readonly _protocol: IHubProtocol;
    private readonly _connection: IConnection;

    private readonly _bufferSize: number = 100_000;

    private _messages: BufferedItem[] = [];
    private _totalMessageCount: number = 0;
    private _waitForSequenceMessage: boolean = false;

    // Message IDs start at 1 and always increment by 1
    private _nextReceivingSequenceId = 1;
    private _latestReceivedSequenceId = 0;
    private _bufferedByteCount: number = 0;
    private _reconnectInProgress: boolean = false;

    private _ackTimerHandle?: any;

    constructor(protocol: IHubProtocol, connection: IConnection, bufferSize: number) {
        this._protocol = protocol;
        this._connection = connection;
        this._bufferSize = bufferSize;
    }

    public async _send(message: HubMessage): Promise<void> {
        const serializedMessage = this._protocol.writeMessage(message);

        let backpressurePromise: Promise<void> = Promise.resolve();

        // Only count invocation messages. Acks, pings, etc. don't need to be resent on reconnect
        if (this._isInvocationMessage(message)) {
            this._totalMessageCount++;
            let backpressurePromiseResolver: (value: void) => void = () => {};
            let backpressurePromiseRejector: (value?: void) => void = () => {};

            if (isArrayBuffer(serializedMessage)) {
                this._bufferedByteCount += serializedMessage.byteLength;
            } else {
                this._bufferedByteCount += serializedMessage.length;
            }

            if (this._bufferedByteCount >= this._bufferSize) {
                backpressurePromise = new Promise((resolve, reject) => {
                    backpressurePromiseResolver = resolve;
                    backpressurePromiseRejector = reject;
                });
            }

            this._messages.push(new BufferedItem(serializedMessage, this._totalMessageCount,
                backpressurePromiseResolver, backpressurePromiseRejector));
        }

        try {
            // If this is set it means we are reconnecting or resending
            // We don't want to send on a disconnected connection
            // And we don't want to send if resend is running since that would mean sending
            // this message twice
            if (!this._reconnectInProgress) {
                await this._connection.send(serializedMessage);
            }
        } catch {
            this._disconnected();
        }
        await backpressurePromise;
    }

    public _ack(ackMessage: AckMessage): void {
        let newestAckedMessage = -1;

        // Find index of newest message being acked
        for (let index = 0; index < this._messages.length; index++) {
            const element = this._messages[index];
            if (element._id <= ackMessage.sequenceId) {
                newestAckedMessage = index;
                if (isArrayBuffer(element._message)) {
                    this._bufferedByteCount -= element._message.byteLength;
                } else {
                    this._bufferedByteCount -= element._message.length;
                }
                // resolve items that have already been sent and acked
                element._resolver();
            } else if (this._bufferedByteCount < this._bufferSize) {
                // resolve items that now fall under the buffer limit but haven't been acked
                element._resolver();
            } else {
                break;
            }
        }

        if (newestAckedMessage !== -1) {
            // We're removing everything including the message pointed to, so add 1
            this._messages = this._messages.slice(newestAckedMessage + 1);
        }
    }

    public _shouldProcessMessage(message: HubMessage): boolean {
        if (this._waitForSequenceMessage) {
            if (message.type !== MessageType.Sequence) {
                return false;
            } else {
                this._waitForSequenceMessage = false;
                return true;
            }
        }

        // No special processing for acks, pings, etc.
        if (!this._isInvocationMessage(message)) {
            return true;
        }

        const currentId = this._nextReceivingSequenceId;
        this._nextReceivingSequenceId++;
        if (currentId <= this._latestReceivedSequenceId) {
            if (currentId === this._latestReceivedSequenceId) {
                // Should only hit this if we just reconnected and the server is sending
                // Messages it has buffered, which would mean it hasn't seen an Ack for these messages
                this._ackTimer();
            }
            // Ignore, this is a duplicate message
            return false;
        }

        this._latestReceivedSequenceId = currentId;

        // Only start the timer for sending an Ack message when we have a message to ack. This also conveniently solves
        // timer throttling by not having a recursive timer, and by starting the timer via a network call (recv)
        this._ackTimer();
        return true;
    }

    public _resetSequence(message: SequenceMessage): void {
        if (message.sequenceId > this._nextReceivingSequenceId) {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            this._connection.stop(new Error("Sequence ID greater than amount of messages we've received."));
            return;
        }

        this._nextReceivingSequenceId = message.sequenceId;
    }

    public _disconnected(): void {
        this._reconnectInProgress = true;
        this._waitForSequenceMessage = true;
    }

    public async _resend(): Promise<void> {
        const sequenceId = this._messages.length !== 0
            ? this._messages[0]._id
            :  this._totalMessageCount + 1;
        await this._connection.send(this._protocol.writeMessage({ type: MessageType.Sequence, sequenceId }));

        // Get a local variable to the _messages, just in case messages are acked while resending
        // Which would slice the _messages array (which creates a new copy)
        const messages = this._messages;
        for (const element of messages) {
            await this._connection.send(element._message);
        }

        this._reconnectInProgress = false;
    }

    public _dispose(error?: Error): void {
        error ??= new Error("Unable to reconnect to server.")

        // Unblock backpressure if any
        for (const element of this._messages) {
            element._rejector(error);
        }
    }

    private _isInvocationMessage(message: HubMessage): boolean {
        // There is no way to check if something implements an interface.
        // So we individually check the messages in a switch statement.
        // To make sure we don't miss any message types we rely on the compiler
        // seeing the function returns a value and it will do the
        // exhaustive check for us on the switch statement, since we don't use 'case default'
        switch (message.type) {
            case MessageType.Invocation:
            case MessageType.StreamItem:
            case MessageType.Completion:
            case MessageType.StreamInvocation:
            case MessageType.CancelInvocation:
                return true;
            case MessageType.Close:
            case MessageType.Sequence:
            case MessageType.Ping:
            case MessageType.Ack:
                return false;
        }
    }

    private _ackTimer(): void {
        if (this._ackTimerHandle === undefined) {
            this._ackTimerHandle = setTimeout(async () => {
                try {
                    if (!this._reconnectInProgress) {
                        await this._connection.send(this._protocol.writeMessage({ type: MessageType.Ack, sequenceId: this._latestReceivedSequenceId }))
                    }
                // Ignore errors, that means the connection is closed and we don't care about the Ack message anymore.
                } catch { }

                clearTimeout(this._ackTimerHandle);
                this._ackTimerHandle = undefined;
            // 1 second delay so we don't spam Ack messages if there are many messages being received at once.
            }, 1000);
        }
    }
}

class BufferedItem {
    constructor(message: string | ArrayBuffer, id: number, resolver: (value: void) => void, rejector: (value?: any) => void) {
        this._message = message;
        this._id = id;
        this._resolver = resolver;
        this._rejector = rejector;
    }

    _message: string | ArrayBuffer;
    _id: number;
    _resolver: (value: void) => void;
    _rejector: (value?: any) => void;
}
