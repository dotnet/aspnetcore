// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IConnection } from "./IConnection";
import { AckMessage, HubMessage, IHubProtocol, MessageType, SequenceMessage } from "./IHubProtocol";
import { isArrayBuffer } from "./Utils";

/** @private */
export class MessageBuffer {
    private readonly _protocol: IHubProtocol;
    private readonly _connection: IConnection;

    private readonly _bufferLimit: number = 10_000;

    private _messages: Key[] = [];
    private _totalMessageCount: number = 0;
    private _waitForSequenceMessage: boolean = false;

    // Message IDs start at 1 and always increment by 1
    private _currentReceivingSequenceId = 1;
    private _latestReceivedSequenceId = -100;
    private _bufferedByteCount: number = 0;

    private _ackTimerHandle?: any;

    // private _backPressurePromise: Promise<void> = Promise.resolve();
    private _resendPromise: Promise<void> = Promise.resolve();
    private _resendResolve!: (value: void) => void;
    private _resendReject!: (value?: any) => void;

    constructor(protocol: IHubProtocol, connection: IConnection) {
        this._protocol = protocol;
        this._connection = connection;
    }

    public async _send(message: HubMessage): Promise<void> {
        if (this._bufferedByteCount > this._bufferLimit) {
            // await this._backPressurePromise;
        }

        await this._resendPromise;

        const serializedMessage = this._protocol.writeMessage(message);

        // Only count invocation messages. Acks, pings, etc. don't need to be resent on reconnect
        if (this._isInvocationMessage(message)) {
            this._totalMessageCount++;
            if (isArrayBuffer(serializedMessage)) {
                this._bufferedByteCount += serializedMessage.byteLength;
            } else {
                this._bufferedByteCount += serializedMessage.length;
            }
            this._messages.push(new Key(serializedMessage, this._totalMessageCount));
        }

        return this._connection.send(serializedMessage);
    }

    public _ack(ackMessage: AckMessage): void {
        let mostSignificantAckedMessage = -1;

        // Find index of newest message being acked
        for (let index = 0; index < this._messages.length; index++) {
            const element = this._messages[index];
            if (element.id <= ackMessage.sequenceId) {
                mostSignificantAckedMessage = index;
                if (isArrayBuffer(element.message)) {
                    this._bufferedByteCount -= element.message.byteLength;
                } else {
                    this._bufferedByteCount -= element.message.length;
                }
            } else {
                break;
            }
        }

        if (mostSignificantAckedMessage !== -1) {
            // We're removing everything including the message pointed to, so add 1
            this._messages = this._messages.slice(mostSignificantAckedMessage + 1);

            // this._promise
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

        // id check
        const currentId = this._currentReceivingSequenceId;
        this._currentReceivingSequenceId++;
        if (currentId <= this._latestReceivedSequenceId) {
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
        if (message.sequenceId > this._currentReceivingSequenceId) {
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            this._connection.stop(new Error("Sequence ID greater than amount of messages we've received."));
        }

        this._currentReceivingSequenceId = message.sequenceId;
    }

    public _disconnected(): void {
        this._resendPromise = new Promise((resolve, reject) => {
            this._resendResolve = resolve;
            this._resendReject = reject;
        });
    }

    public async _resend(doSend: boolean): Promise<void> {
        if (!doSend) {
            // We need to unblock the promise in the case where we aren't able to reconnect to the server
            this._resendReject(new Error(""));
            return;
        }

        try {
            let sequenceId = this._totalMessageCount + 1;
            if (this._messages.length !== 0) {
                sequenceId = this._messages[0].id;
            }
            await this._connection.send(this._protocol.writeMessage({ type: MessageType.Sequence, sequenceId: sequenceId }));

            for (let index = 0; index < this._messages.length; index++) {
                const element = this._messages[index];

                await this._connection.send(element.message);
            }
        } catch (e) {
            this._resendReject(e);
        } finally {
            this._resendResolve();
        }
    }

    private _isInvocationMessage(message: HubMessage) {
        // There is no way to check if something implements an interface.
        // We just check if the type is between 1 and 5 as those are currently the only messages worth resending right now.
        return message.type >= MessageType.Invocation && message.type <= MessageType.CancelInvocation;
    }

    private _ackTimer(): void {
        if (this._ackTimerHandle === undefined) {
            this._ackTimerHandle = setTimeout(async () => {
                try {
                    await this._connection.send(this._protocol.writeMessage({ type: MessageType.Ack, sequenceId: this._latestReceivedSequenceId }))
                // Ignore errors, that means the connection is closed and we don't care about the Ack message anymore.
                } catch { }

                clearTimeout(this._ackTimerHandle);
                this._ackTimerHandle = undefined;
            // 1 second delay so we don't spam Ack messages if there are many messages being received at once.
            }, 1000);
        }
    }
}

class Key {
    constructor(message: string | ArrayBuffer, id: number) {
        this.message = message;
        this.id = id;
    }

    message: string | ArrayBuffer;
    id: number;
}
