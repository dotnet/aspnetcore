// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ITransport } from "signalr/src/ITransport";

export class TestTransport implements ITransport {
    public connect(): Promise<void> {
        return Promise.resolve();
    }

    public send(): Promise<void> {
        return Promise.resolve();
    }

    public stop(): Promise<void> {
        return Promise.resolve();
    }

    private _onReceive: ((data: string | ArrayBuffer) => void) | null = null;
    private _onClose: ((error?: Error | undefined) => void) | null = null;

    public get onReceive(): ((data: string | ArrayBuffer) => void) | null {
        return this._onReceive;
    }
    public set onReceive(value: ((data: string | ArrayBuffer) => void) | null) {
        this._onReceive = value;
    }

    public get onClose(): ((error?: Error | undefined) => void) | null {
        return this._onClose;
    }
    public set onClose(value: ((error?: Error | undefined) => void) | null) {
        this._onClose = value;
    }

    public get onreceive(): ((data: string | ArrayBuffer) => void) | null {
        return this._onReceive;
    }
    public set onreceive(value: ((data: string | ArrayBuffer) => void) | null) {
        this._onReceive = value;
    }

    public get onclose(): ((error?: Error | undefined) => void) | null {
        return this._onClose;
    }
    public set onclose(value: ((error?: Error | undefined) => void) | null) {
        this._onClose = value;
    }
}
