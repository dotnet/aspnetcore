// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { CompletionMessage, HubMessage, IHubProtocol, InvocationMessage, MessageType, StreamItemMessage } from "./IHubProtocol";
import { ILogger, LogLevel } from "./ILogger";
import { TransferFormat } from "./ITransport";
import { NullLogger } from "./Loggers";
import { TextMessageFormat } from "./TextMessageFormat";

const JSON_HUB_PROTOCOL_NAME: string = "json";

/** Implements the JSON Hub Protocol. */
export class JsonHubProtocol implements IHubProtocol {

    /** @inheritDoc */
    public readonly name: string = JSON_HUB_PROTOCOL_NAME;
    /** @inheritDoc */
    public readonly version: number = 1;

    /** @inheritDoc */
    public readonly transferFormat: TransferFormat = TransferFormat.Text;

    /** Creates an array of {@link @microsoft/signalr.HubMessage} objects from the specified serialized representation.
     *
     * @param {string} input A string containing the serialized representation.
     * @param {ILogger} logger A logger that will be used to log messages that occur during parsing.
     */
    public parseMessages(input: string, logger: ILogger): HubMessage[] {
        // The interface does allow "ArrayBuffer" to be passed in, but this implementation does not. So let's throw a useful error.
        if (typeof input !== "string") {
            throw new Error("Invalid input for JSON hub protocol. Expected a string.");
        }

        if (!input) {
            return [];
        }

        if (logger === null) {
            logger = NullLogger.instance;
        }

        // Parse the messages
        const messages = TextMessageFormat.parse(input);

        const hubMessages = [];
        for (const message of messages) {
            const parsedMessage = JSON.parse(message) as HubMessage;
            if (typeof parsedMessage.type !== "number") {
                throw new Error("Invalid payload.");
            }
            switch (parsedMessage.type) {
                case MessageType.Invocation:
                    this.isInvocationMessage(parsedMessage);
                    break;
                case MessageType.StreamItem:
                    this.isStreamItemMessage(parsedMessage);
                    break;
                case MessageType.Completion:
                    this.isCompletionMessage(parsedMessage);
                    break;
                case MessageType.Ping:
                    // Single value, no need to validate
                    break;
                case MessageType.Close:
                    // All optional values, no need to validate
                    break;
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    logger.log(LogLevel.Information, "Unknown message type '" + parsedMessage.type + "' ignored.");
                    continue;
            }
            hubMessages.push(parsedMessage);
        }

        return hubMessages;
    }

    /** Writes the specified {@link @microsoft/signalr.HubMessage} to a string and returns it.
     *
     * @param {HubMessage} message The message to write.
     * @returns {string} A string containing the serialized representation of the message.
     */
    public writeMessage(message: HubMessage): string {
        return TextMessageFormat.write(JSON.stringify(message));
    }

    private isInvocationMessage(message: InvocationMessage): void {
        this.assertNotEmptyString(message.target, "Invalid payload for Invocation message.");

        if (message.invocationId !== undefined) {
            this.assertNotEmptyString(message.invocationId, "Invalid payload for Invocation message.");
        }
    }

    private isStreamItemMessage(message: StreamItemMessage): void {
        this.assertNotEmptyString(message.invocationId, "Invalid payload for StreamItem message.");

        if (message.item === undefined) {
            throw new Error("Invalid payload for StreamItem message.");
        }
    }

    private isCompletionMessage(message: CompletionMessage): void {
        if (message.result && message.error) {
            throw new Error("Invalid payload for Completion message.");
        }

        if (!message.result && message.error) {
            this.assertNotEmptyString(message.error, "Invalid payload for Completion message.");
        }

        this.assertNotEmptyString(message.invocationId, "Invalid payload for Completion message.");
    }

    private assertNotEmptyString(value: any, errorMessage: string): void {
        if (typeof value !== "string" || value === "") {
            throw new Error(errorMessage);
        }
    }
}
