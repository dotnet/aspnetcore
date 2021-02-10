// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { TextMessageFormat } from "./TextMessageFormat";
import { isArrayBuffer } from "./Utils";

/** @private */
export interface HandshakeRequestMessage {
    readonly protocol: string;
    readonly version: number;
}

/** @private */
export interface HandshakeResponseMessage {
    readonly error: string;
}

/** @private */
export class HandshakeProtocol {
    // Handshake request is always JSON
    public writeHandshakeRequest(handshakeRequest: HandshakeRequestMessage): string {
        return TextMessageFormat.write(JSON.stringify(handshakeRequest));
    }

    public parseHandshakeResponse(data: any): [any, HandshakeResponseMessage] {
        let responseMessage: HandshakeResponseMessage;
        let messageData: string;
        let remainingData: any;

        if (isArrayBuffer(data)) {
            // Format is binary but still need to read JSON text from handshake response
            const binaryData = new Uint8Array(data);
            const separatorIndex = binaryData.indexOf(TextMessageFormat.RecordSeparatorCode);
            if (separatorIndex === -1) {
                throw new Error("Message is incomplete.");
            }

            // content before separator is handshake response
            // optional content after is additional messages
            const responseLength = separatorIndex + 1;
            messageData = String.fromCharCode.apply(null, binaryData.slice(0, responseLength));
            remainingData = (binaryData.byteLength > responseLength) ? binaryData.slice(responseLength).buffer : null;
        } else {
            const textData: string = data;
            const separatorIndex = textData.indexOf(TextMessageFormat.RecordSeparator);
            if (separatorIndex === -1) {
                throw new Error("Message is incomplete.");
            }

            // content before separator is handshake response
            // optional content after is additional messages
            const responseLength = separatorIndex + 1;
            messageData = textData.substring(0, responseLength);
            remainingData = (textData.length > responseLength) ? textData.substring(responseLength) : null;
        }

        // At this point we should have just the single handshake message
        const messages = TextMessageFormat.parse(messageData);
        responseMessage = JSON.parse(messages[0]);

        // multiple messages could have arrived with handshake
        // return additional data to be parsed as usual, or null if all parsed
        return [remainingData, responseMessage];
    }
}
