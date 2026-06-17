// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { CircuitStartOptions } from './Circuits/CircuitStartOptions';
import { SsrStartOptions } from './SsrStartOptions';
import { LogLevel } from './Logging/Logger';

export interface WebStartOptions {
  enableClassicInitializers?: boolean;
  circuit: CircuitStartOptions;
  webAssembly: WebAssemblyStartOptions;
  logLevel?: LogLevel;
  ssr: SsrStartOptions;
}
