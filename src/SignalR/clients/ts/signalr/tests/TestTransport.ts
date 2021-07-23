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

    public onreceive: ((data: string | ArrayBuffer) => void) | null = null;
    public onclose: ((error?: Error | undefined) => void) | null = null;
}
