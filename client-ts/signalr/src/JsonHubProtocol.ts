// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HubMessage, IHubProtocol, ProtocolType } from "./IHubProtocol";
import { TextMessageFormat } from "./TextMessageFormat";

export const JSON_HUB_PROTOCOL_NAME: string = "json";

export class JsonHubProtocol implements IHubProtocol {

    public readonly name: string = JSON_HUB_PROTOCOL_NAME;

    public readonly type: ProtocolType = ProtocolType.Text;

    public parseMessages(input: string): HubMessage[] {
        if (!input) {
            return [];
        }

        // Parse the messages
        const messages = TextMessageFormat.parse(input);
        const hubMessages = [];
        for (const message of messages) {
            hubMessages.push(JSON.parse(message));
        }

        return hubMessages;
    }

    public writeMessage(message: HubMessage): string {
        return TextMessageFormat.write(JSON.stringify(message));
    }
}
