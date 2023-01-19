import { MessagePackOptions } from "./MessagePackOptions";
import { HubMessage, IHubProtocol, ILogger, TransferFormat } from "@microsoft/signalr";
/** Implements the MessagePack Hub Protocol */
export declare class MessagePackHubProtocol implements IHubProtocol {
    /** The name of the protocol. This is used by SignalR to resolve the protocol between the client and server. */
    readonly name: string;
    /** The version of the protocol. */
    readonly version: number;
    /** The TransferFormat of the protocol. */
    readonly transferFormat: TransferFormat;
    private readonly _errorResult;
    private readonly _voidResult;
    private readonly _nonVoidResult;
    private readonly _encoder;
    private readonly _decoder;
    /**
     *
     * @param messagePackOptions MessagePack options passed to @msgpack/msgpack
     */
    constructor(messagePackOptions?: MessagePackOptions);
    /** Creates an array of HubMessage objects from the specified serialized representation.
     *
     * @param {ArrayBuffer} input An ArrayBuffer containing the serialized representation.
     * @param {ILogger} logger A logger that will be used to log messages that occur during parsing.
     */
    parseMessages(input: ArrayBuffer, logger: ILogger): HubMessage[];
    /** Writes the specified HubMessage to an ArrayBuffer and returns it.
     *
     * @param {HubMessage} message The message to write.
     * @returns {ArrayBuffer} An ArrayBuffer containing the serialized representation of the message.
     */
    writeMessage(message: HubMessage): ArrayBuffer;
    private _parseMessage;
    private _createCloseMessage;
    private _createPingMessage;
    private _createInvocationMessage;
    private _createStreamItemMessage;
    private _createCompletionMessage;
    private _writeInvocation;
    private _writeStreamInvocation;
    private _writeStreamItem;
    private _writeCompletion;
    private _writeCancelInvocation;
    private _readHeaders;
}
//# sourceMappingURL=MessagePackHubProtocol.d.ts.map