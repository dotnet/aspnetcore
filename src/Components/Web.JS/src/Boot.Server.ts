// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { CircuitStartOptions, resolveOptions } from './Platform/Circuits/CircuitStartOptions';
import { setCircuitOptions, startServer } from './Boot.Server.Common';
import { ServerComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';
import { DotNet } from '@microsoft/dotnet-js-interop';
import { InitialRootComponentsList } from './Services/InitialRootComponentsList';
import { JSEventRegistry } from './Services/JSEventRegistry';

let started = false;

function boot(userOptions?: Partial<CircuitStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const configuredOptions = resolveOptions(userOptions);
  setCircuitOptions(Promise.resolve(configuredOptions || {}));

  JSEventRegistry.create(Blazor);
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const components = new InitialRootComponentsList(serverComponents);
  return startServer(components);
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
