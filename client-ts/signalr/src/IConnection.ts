// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ConnectionClosed, DataReceived } from "./Common";
import { ITransport, TransferFormat, TransportType } from "./Transports";

export interface IConnection {
    readonly features: any;

    start(transferFormat: TransferFormat): Promise<void>;
    send(data: any): Promise<void>;
    stop(error?: Error): Promise<void>;

    onreceive: DataReceived;
    onclose: ConnectionClosed;
}
