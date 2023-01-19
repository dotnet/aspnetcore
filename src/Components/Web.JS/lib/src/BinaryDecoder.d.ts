export declare function readInt32LE(buffer: Uint8Array, position: number): any;
export declare function readUint32LE(buffer: Uint8Array, position: number): any;
export declare function readUint64LE(buffer: Uint8Array, position: number): any;
export declare function readLEB128(buffer: Uint8Array, position: number): number;
export declare function numLEB128Bytes(value: number): 1 | 2 | 3 | 4;
