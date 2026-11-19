// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { TransferFormat } from "./ITransport";

/** @private */
export interface IConnection {
    readonly features: any;
    readonly connectionId?: string;

    baseUrl: string;

    start(transferFormat: TransferFormat): Promise<void>;
    send(data: string | ArrayBuffer): Promise<void>;
    stop(error?: Error | unknown): Promise<void>;

    onReceive?: ((data: string | ArrayBuffer) => void) | null;
    onClose?: ((error?: Error) => void) | null;
    /** @deprecated Use {@link IConnection.onReceive} instead. */
    onreceive: ((data: string | ArrayBuffer) => void) | null;
    /** @deprecated Use {@link IConnection.onClose} instead. */
    onclose: ((error?: Error) => void) | null;
}
