import { TextMessageFormat } from "./Formatters";
import { IHubProtocol, HubMessage } from "./IHubProtocol";

export class JsonHubProtocol implements IHubProtocol {
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