// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { UnitedStartOptions } from './Platform/Circuits/UnitedStartOptions';
import { startCircuit } from './Boot.Server.Common';
import { discoverComponents, ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';

let started = false;

async function boot(options?: Partial<UnitedStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  // Only set up a circuit if the page actually contains interactive server components
  // Later on, when we add progressive enhancement to navigation, we will also want to
  // auto-close circuits when the last root component is removed.
  const components = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  if (components.length) {
    await startCircuit(options?.circuit);
  }
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
