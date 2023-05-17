// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { CircuitStartOptions } from './Platform/Circuits/CircuitStartOptions';
import { startCircuit } from './Boot.Server.Common';

let started = false;

function boot(userOptions?: Partial<CircuitStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  return startCircuit(userOptions);
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
