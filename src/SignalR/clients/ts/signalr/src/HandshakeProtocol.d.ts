/** @private */
export interface HandshakeRequestMessage {
    readonly protocol: string;
    readonly version: number;
}
/** @private */
export interface HandshakeResponseMessage {
    readonly error: string;
    readonly minorVersion: number;
}
/** @private */
export declare class HandshakeProtocol {
    writeHandshakeRequest(handshakeRequest: HandshakeRequestMessage): string;
    parseHandshakeResponse(data: any): [any, HandshakeResponseMessage];
}
//# sourceMappingURL=HandshakeProtocol.d.ts.map