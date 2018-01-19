// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { TextMessageFormat } from "./TextMessageFormat";
import { IHubProtocol, ProtocolType, HubMessage } from "./IHubProtocol";

export const JSON_HUB_PROTOCOL_NAME: string = "json";

export class JsonHubProtocol implements IHubProtocol {

    readonly name: string = JSON_HUB_PROTOCOL_NAME;

    readonly type: ProtocolType = ProtocolType.Text;

    parseMessages(input: string): HubMessage[] {
        if (!input) {
            return [];
        }

        // Parse the messages
        let messages = TextMessageFormat.parse(input);
        let hubMessages = [];
        for (var i = 0; i < messages.length; ++i) {
            hubMessages.push(JSON.parse(messages[i]));
        }

        return hubMessages;
    }

    writeMessage(message: HubMessage): string {
        return TextMessageFormat.write(JSON.stringify(message));
    }
}