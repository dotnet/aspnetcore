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
import { attachProgressivelyEnhancedNavigationListener, detachProgressivelyEnhancedNavigationListener } from './Services/NavigationEnhancement';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';

let started = false;
let webStartOptions: Partial<WebStartOptions> | undefined;

async function boot(options?: Partial<WebStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  webStartOptions = options;

  attachStreamingRenderingListener(options?.ssr);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(activateInteractiveComponents);
  }

  // TODO: It's not enough only to activate interactive components after documentReady. We should also
  // try activating them each time there's an SSR update, and even at the point when the initial HTML
  // is loaded but we're still waiting for the first streaming SSR update.
  // This is covered by #48763 and #48764
  await waitForDocumentReady();
  await activateInteractiveComponents();
}

async function activateInteractiveComponents() {
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  if (serverComponents.length) {
    // TEMPORARY until https://github.com/dotnet/aspnetcore/issues/48763 is implemented
    // As soon we we see you have interactive components, we'll stop doing enhanced nav even if you don't have an interactive router
    // This is because, otherwise, we would need a way to add new interactive root components to an existing circuit and that's #48763
    detachProgressivelyEnhancedNavigationListener();

    await startCircuit(webStartOptions?.circuit, serverComponents);
  }

  if (webAssemblyComponents.length) {
    // TEMPORARY until https://github.com/dotnet/aspnetcore/issues/48763 is implemented
    // As soon we we see you have interactive components, we'll stop doing enhanced nav even if you don't have an interactive router
    // This is because, otherwise, we would need a way to add new interactive root components to an existing WebAssembly runtime and that's #48763
    detachProgressivelyEnhancedNavigationListener();

    await startWebAssembly(webStartOptions?.webAssembly, webAssemblyComponents);
  }
}

async function waitForDocumentReady() {
  // The other two possible states are 'interactive' and 'complete', and in both cases we're ready to proceed
  if (document.readyState === 'loading') {
    let resolver: Function;
    const result = new Promise(resolve => { resolver = resolve });
    document.addEventListener('DOMContentLoaded', () => resolver());
    return result;
  }
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
