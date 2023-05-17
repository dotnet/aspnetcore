// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { activatePrerenderedComponents, startCircuit } from './Boot.Server.Common';
import { startWebAssembly } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';

let booted = false;
let hasStartedWebAssembly = false;
let hasStartedServer = false;

async function boot(): Promise<void> {
  if (booted) {
    throw new Error('Blazor has already started.');
  }
  booted = true;
  await activateInteractiveComponents();

  attachStreamingRenderingListener();
}

async function activateInteractiveComponents() {
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  if (serverComponents.length) {
    if (!hasStartedServer) {
      hasStartedServer = true;
      await startCircuit({}, serverComponents); // TODO: Unified "options" object.
    } else {
      await activatePrerenderedComponents(serverComponents); // TODO
    }
  }

  if (webAssemblyComponents.length) {
    if (!hasStartedWebAssembly) {
      hasStartedWebAssembly = true;
      await startWebAssembly({}, webAssemblyComponents); // TODO: Unified "options" object.
    } else {
      // TODO: Activate webassembly prerendered components after WASM startup.
      throw new Error('TODO: Activate webassembly prerendered components.');
    }
  }

  // TODO: Navigation setup here?
}

Blazor.start = boot;
// window['Blazor'] = { start: boot }; // Temporary API stub until we include interactive features

if (shouldAutoStart()) {
  boot();
}
