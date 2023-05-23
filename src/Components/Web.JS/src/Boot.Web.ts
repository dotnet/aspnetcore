// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { DotNet } from '@microsoft/dotnet-js-interop';
import { startCircuit } from './Boot.Server.Common';
import { startWebAssembly } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { WebStartOptions } from './Platform/WebStartOptions';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';

let started = false;

async function boot(options?: Partial<WebStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  await activateInteractiveComponents(options);

  attachStreamingRenderingListener();
}

async function activateInteractiveComponents(options?: Partial<WebStartOptions>) {
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  if (serverComponents.length) {
    await startCircuit(options?.circuit, serverComponents);
  }

  if (webAssemblyComponents.length) {
    await startWebAssembly(options?.webAssembly, webAssemblyComponents);
  }
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
