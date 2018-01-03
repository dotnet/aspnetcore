// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient"
import { TransportType, ITransport } from "./Transports"
import { ILogger, LogLevel } from "./ILogger";

export interface IHttpConnectionOptions {
    httpClient?: HttpClient;
    transport?: TransportType | ITransport;
    logger?: ILogger | LogLevel;
    accessToken?: () => string;
}
