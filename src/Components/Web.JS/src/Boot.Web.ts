// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { DotNet } from '@microsoft/dotnet-js-interop';
import { addServerDescriptor, processRemovedServerDescriptors, processUpdatedServerDescriptors, startCircuit } from './Boot.Server.Common';
import { startWebAssembly } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { WebStartOptions } from './Platform/WebStartOptions';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { NavigationEnhancementCallbacks, attachProgressivelyEnhancedNavigationListener } from './Services/NavigationEnhancement';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { DescriptorHandler, attachComponentDescriptorHandler, processComponentDescriptors } from './Rendering/DomMerging/BoundarySync';

let started = false;
let isPerformingEnhnacedNavigation = false;
let webStartOptions: Partial<WebStartOptions> | undefined;
let lastActiveElement: Element | null = null;

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  webStartOptions = options;

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    beforeEnhancedNavigation,
    beforeDocumentUpdated,
    afterDocumentUpdated,
    afterEnhancedNavigation,
  };

  const descriptorHandler: DescriptorHandler = {
    onDescriptorAdded,
  };

  attachComponentDescriptorHandler(descriptorHandler);
  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  processComponentDescriptors(document);

  return Promise.resolve();
}

function onDescriptorAdded(descriptor: ServerComponentDescriptor | WebAssemblyComponentDescriptor) {
  switch (descriptor.type) {
    case 'server':
      // Start the circuit now so that it's more likely to be ready by the time interactivity actually starts.
      startCircuitIfNotStarted();
      addServerDescriptor(descriptor);
      break;
    case 'webassembly':
      startWebAssemblyIfNotStarted();
      // TODO: WebAssembly.
      break;
  }
}

function beforeEnhancedNavigation() {
  isPerformingEnhnacedNavigation = true;
}

function beforeDocumentUpdated() {
  lastActiveElement = document.activeElement;
}

function afterDocumentUpdated() {
  if ((lastActiveElement instanceof HTMLElement) && lastActiveElement !== document.activeElement) {
    lastActiveElement.focus();
  }

  lastActiveElement = null;
  handleUpdatedDescriptors();
}

function afterEnhancedNavigation() {
  isPerformingEnhnacedNavigation = false;
  handleUpdatedDescriptors();
}

function handleUpdatedDescriptors() {
  processRemovedServerDescriptors();

  if (!isPerformingEnhnacedNavigation) {
    // We avoid adding new root components during enhanced navigations.

    // TODO: Handle 'activate' and 'update' seprately.
    // We might want to update parameters of existing components
    // without activating new ones.
    processUpdatedServerDescriptors();
  }
}

let circuitStarted = false;
async function startCircuitIfNotStarted() {
  if (circuitStarted) {
    return;
  }

  circuitStarted = true;
  await startCircuit(webStartOptions?.circuit);
  handleUpdatedDescriptors();
}

let webAssemblyStarted = false;
async function startWebAssemblyIfNotStarted() {
  if (webAssemblyStarted) {
    return;
  }

  webAssemblyStarted = true;
  await startWebAssembly(webStartOptions?.webAssembly);
  handleUpdatedDescriptors();
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
