// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IHubProtocol } from "./IHubProtocol"
import { ILogger, LogLevel } from "./ILogger"

export interface IHubConnectionOptions {
    protocol?: IHubProtocol;
    logging?: ILogger | LogLevel;
}
