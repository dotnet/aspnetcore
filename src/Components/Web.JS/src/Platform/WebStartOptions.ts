// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebAssemblyStartOptions } from './WebAssemblyStartOptions';
import { CircuitStartOptions } from './Circuits/CircuitStartOptions';
import { SsrStartOptions } from './SsrStartOptions';

export interface WebStartOptions {
  circuit: CircuitStartOptions;
  webAssembly: WebAssemblyStartOptions;
  ssr: SsrStartOptions;
}
