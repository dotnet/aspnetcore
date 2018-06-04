import { HubConnection } from "./HubConnection";
import { MessageType } from "./IHubProtocol";

export class UploadStream {
    private connection: HubConnection;

    public readonly streamId: string;
    public readonly placeholder: object;

    constructor(connection: HubConnection) {
        this.connection = connection;
        this.streamId = connection.nextStreamId();
        this.placeholder = {streamId: this.streamId};
    }

    public write(item: any): Promise<void> {
        return this.connection.sendWithProtocol(this.connection.createStreamDataMessage(this.streamId, item));
    }

    public complete(error?: string): Promise<void> {
        if (error) {
            return this.connection.sendWithProtocol({ type: MessageType.StreamComplete, streamId: this.streamId, error });
        } else {
            return this.connection.sendWithProtocol({ type: MessageType.StreamComplete, streamId: this.streamId });
        }
    }
}
