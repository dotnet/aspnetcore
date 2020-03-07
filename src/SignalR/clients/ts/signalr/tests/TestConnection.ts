// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IConnection } from "../src/IConnection";
import { TextMessageFormat } from "../src/TextMessageFormat";

export class TestConnection implements IConnection {
    public baseUrl: string;
    public readonly features: any = {};
    public connectionId?: string;

    public onreceive: ((data: string | ArrayBuffer) => void) | null;
    public onclose: ((error?: Error) => void) | null;

    public sentData: any[];
    public parsedSentData: any[];
    public lastInvocationId: string | null;

    private autoHandshake: boolean | null;

    constructor(autoHandshake: boolean = true, hasInherentKeepAlive: boolean = false) {
        this.onreceive = null;
        this.onclose = null;
        this.sentData = [];
        this.parsedSentData = [];
        this.lastInvocationId = null;
        this.autoHandshake = autoHandshake;
        this.baseUrl = "http://example.com";
        this.features.inherentKeepAlive = hasInherentKeepAlive;
    }

    public start(): Promise<void> {
        return Promise.resolve();
    }

    public send(data: any): Promise<void> {
        const invocation = TextMessageFormat.parse(data)[0];
        const parsedInvocation = JSON.parse(invocation);
        const invocationId = parsedInvocation.invocationId;
        if (parsedInvocation.protocol && parsedInvocation.version && this.autoHandshake) {
            this.receiveHandshakeResponse();
        }
        if (invocationId) {
            this.lastInvocationId = invocationId;
        }
        if (this.sentData) {
            this.sentData.push(invocation);
            this.parsedSentData.push(parsedInvocation);
        } else {
            this.sentData = [invocation];
            this.parsedSentData = [parsedInvocation];
        }
        return Promise.resolve();
    }

    public stop(error?: Error): Promise<void> {
        if (this.onclose) {
            this.onclose(error);
        }
        return Promise.resolve();
    }

    public receiveHandshakeResponse(error?: string): void {
        this.receive({ error });
    }

    public receive(data: any): void {
        const payload = JSON.stringify(data);
        this.invokeOnReceive(TextMessageFormat.write(payload));
    }

    public receiveText(data: string) {
        this.invokeOnReceive(data);
    }

    public receiveBinary(data: ArrayBuffer) {
        this.invokeOnReceive(data);
    }

    private invokeOnReceive(data: string | ArrayBuffer) {
        if (this.onreceive) {
            this.onreceive(data);
        }
    }
}
