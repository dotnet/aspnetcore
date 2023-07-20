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
import { NavigationEnhancementCallbacks, attachProgressivelyEnhancedNavigationListener, isPerformingEnhancedPageLoad } from './Services/NavigationEnhancement';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { RootComponentManager } from './Services/RootComponentManager';
import { WebRendererId } from './Rendering/WebRendererId';
import { DescriptorHandler, attachComponentDescriptorHandler, registerAllComponentDescriptors } from './Rendering/DomMerging/DomSync';
import { waitForRendererAttached } from './Rendering/WebRendererInteropMethods';

let started = false;
let webStartOptions: Partial<WebStartOptions> | undefined;

const circuitRootComponents = new RootComponentManager(WebRendererId.Server);
const webAssemblyRootComponents = new RootComponentManager(WebRendererId.WebAssembly);

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  webStartOptions = options;

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    documentUpdated: handleUpdatedComponentDescriptors,
  };

  const descriptorHandler: DescriptorHandler = {
    registerComponentDescriptor,
  };

  attachComponentDescriptorHandler(descriptorHandler);
  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  registerAllComponentDescriptors(document);

  return Promise.resolve();
}

function registerComponentDescriptor(descriptor: ServerComponentDescriptor | WebAssemblyComponentDescriptor) {
  switch (descriptor.type) {
    case 'server':
      startCircuitIfNotStarted();
      circuitRootComponents.registerComponentDescriptor(descriptor);
      break;
    case 'webassembly':
      startWebAssemblyIfNotStarted();
      webAssemblyRootComponents.registerComponentDescriptor(descriptor);
      break;
  }
}

function handleUpdatedComponentDescriptors() {
  const shouldAddNewRootComponents = !isPerformingEnhancedPageLoad();
  circuitRootComponents.handleUpdatedRootComponents(shouldAddNewRootComponents);
  webAssemblyRootComponents.handleUpdatedRootComponents(shouldAddNewRootComponents);
}

let circuitStarted = false;
async function startCircuitIfNotStarted() {
  if (circuitStarted) {
    return;
  }

  circuitStarted = true;
  await startCircuit(webStartOptions?.circuit, circuitRootComponents);
  await waitForRendererAttached(WebRendererId.Server);
  handleUpdatedComponentDescriptors();
}

let webAssemblyStarted = false;
async function startWebAssemblyIfNotStarted() {
  if (webAssemblyStarted) {
    return;
  }

  webAssemblyStarted = true;
  await startWebAssembly(webStartOptions?.webAssembly, webAssemblyRootComponents);
  await waitForRendererAttached(WebRendererId.WebAssembly);
  handleUpdatedComponentDescriptors();
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
