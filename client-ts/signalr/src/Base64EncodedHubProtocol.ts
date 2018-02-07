// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HubMessage, IHubProtocol, ProtocolType } from "./IHubProtocol";

export class Base64EncodedHubProtocol implements IHubProtocol {
    private wrappedProtocol: IHubProtocol;

    constructor(protocol: IHubProtocol) {
        this.wrappedProtocol = protocol;
        this.name = this.wrappedProtocol.name;
        this.type = ProtocolType.Text;
    }

    public readonly name: string;
    public readonly type: ProtocolType;

    public parseMessages(input: any): HubMessage[] {
        // The format of the message is `size:message;`
        const pos = input.indexOf(":");
        if (pos === -1 || input[input.length - 1] !== ";") {
            throw new Error("Invalid payload.");
        }

        const lenStr = input.substring(0, pos);
        if (!/^[0-9]+$/.test(lenStr)) {
            throw new Error(`Invalid length: '${lenStr}'`);
        }

        const messageSize = parseInt(lenStr, 10);
        // 2 accounts for ':' after message size and trailing ';'
        if (messageSize !== input.length - pos - 2) {
            throw new Error("Invalid message size.");
        }

        const encodedMessage = input.substring(pos + 1, input.length - 1);

        // atob/btoa are browsers APIs but they can be polyfilled. If this becomes problematic we can use
        // base64-js module
        const s = atob(encodedMessage);
        const payload = new Uint8Array(s.length);
        for (let i = 0; i < payload.length; i++) {
            payload[i] = s.charCodeAt(i);
        }
        return this.wrappedProtocol.parseMessages(payload.buffer);
    }

    public writeMessage(message: HubMessage): any {
        const payload = new Uint8Array(this.wrappedProtocol.writeMessage(message));
        let s = "";
        for (let i = 0; i < payload.byteLength; i++) {
            s += String.fromCharCode(payload[i]);
        }
        // atob/btoa are browsers APIs but they can be polyfilled. If this becomes problematic we can use
        // base64-js module
        const encodedMessage = btoa(s);

        return `${encodedMessage.length.toString()}:${encodedMessage};`;
    }
}
