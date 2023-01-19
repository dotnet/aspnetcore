export declare function sendAttachPage(baseUrl: string, startUrl: string): void;
export declare function sendRenderCompleted(batchId: number, errorOrNull: string | null): void;
export declare function sendBeginInvokeDotNetFromJS(callId: number, assemblyName: string | null, methodIdentifier: string, dotNetObjectId: number | null, argsJson: string): void;
export declare function sendEndInvokeJSFromDotNet(asyncHandle: number, succeeded: boolean, argsJson: any): void;
export declare function sendByteArray(id: number, data: Uint8Array): void;
export declare function sendLocationChanged(uri: string, state: string | undefined, intercepted: boolean): Promise<void>;
export declare function sendLocationChanging(callId: number, uri: string, state: string | undefined, intercepted: boolean): Promise<void>;
