// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { UnitedStartOptions } from './Platform/Circuits/UnitedStartOptions';
import { startCircuit } from './Boot.Server.Common';
import { discoverComponents, ServerComponentDescriptor, WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { startWebAssembly } from './Boot.WebAssembly.Common';

let started = false;

async function boot(options?: Partial<UnitedStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  if (serverComponents.length && webAssemblyComponents.length) {
    throw new Error('TODO: Support having both Server and WebAssembly components at the same time. Not doing that currently as it overlaps with a different prototype.');
  }

  // Only set up a circuit if the page actually contains interactive server components
  // Later on, when we add progressive enhancement to navigation, we will also want to
  // auto-close circuits when the last root component is removed.
  if (serverComponents.length) {
    await startCircuit(options?.circuit);
  }

  if (webAssemblyComponents.length) {
    await startWebAssembly(options?.webAssembly);
  }
}

Blazor.start = boot;

if (shouldAutoStart()) {
  boot();
}
