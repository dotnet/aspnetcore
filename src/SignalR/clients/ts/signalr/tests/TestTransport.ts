// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ITransport } from "signalr/src/ITransport";

export class TestTransport implements ITransport {
    public connect() {
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
