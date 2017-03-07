export enum MessageType {
    Text,
    Binary,
    Close,
    Error
}

export class Message {
    public type: MessageType;
    public content: ArrayBuffer | string;

    constructor(type: MessageType, content: ArrayBuffer | string) {
        this.type = type;
        this.content = content;
    }
}