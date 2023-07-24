// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { DotNet } from '@microsoft/dotnet-js-interop';
import { setCircuitOptions, startCircuit } from './Boot.Server.Common';
import { loadWebAssemblyPlatform, setWebAssemblyOptions, startWebAssembly } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { WebStartOptions } from './Platform/WebStartOptions';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { NavigationEnhancementCallbacks, attachProgressivelyEnhancedNavigationListener, isPerformingEnhancedPageLoad } from './Services/NavigationEnhancement';
import { ComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { RootComponentManager, attachAutoModeResolver } from './Services/RootComponentManager';
import { DescriptorHandler, attachComponentDescriptorHandler, registerAllComponentDescriptors } from './Rendering/DomMerging/DomSync';
import { waitForRendererAttached } from './Rendering/WebRendererInteropMethods';
import { WebRendererId } from './Rendering/WebRendererId';

enum WebAssemblyLoadingState {
  None = 0,
  Loading = 1,
  Loaded = 2,
  Starting = 3,
  Started = 4,
}

let started = false;
let hasCircuitStarted = false;
let webAssemblyLoadingState = WebAssemblyLoadingState.None;
let autoModeTimeoutState: undefined | 'waiting' | 'timed out';
const autoModeWebAssemblyTimeoutMilliseconds = 100;

const rootComponentManager = new RootComponentManager();

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  setCircuitOptions(options?.circuit);
  setWebAssemblyOptions(options?.webAssembly);

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    documentUpdated: handleUpdatedComponentDescriptors,
  };

  const descriptorHandler: DescriptorHandler = {
    registerComponentDescriptor,
  };

  attachComponentDescriptorHandler(descriptorHandler);
  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);
  attachAutoModeResolver(resolveAutoMode);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  registerAllComponentDescriptors(document);
  handleUpdatedComponentDescriptors();

  return Promise.resolve();
}

function resolveAutoMode(): 'server' | 'webassembly' | null {
  if (webAssemblyLoadingState >= WebAssemblyLoadingState.Loaded) {
    // The WebAssembly runtime has loaded or is actively starting, so we'll use
    // WebAssembly for the component in question. We'll also start
    // the WebAssembly runtime if it hasn't already.
    startWebAssemblyIfNotStarted();
    return 'webassembly';
  }

  if (autoModeTimeoutState === 'timed out') {
    // We've waited too long for WebAssembly to initialize, so we'll use the Server
    // render mode for the component in question. At some point if the WebAssembly
    // runtime finishes loading, we'll start using it again due to the earlier
    // check in this function.
    startCircuitIfNotStarted();
    return 'server';
  }

  if (autoModeTimeoutState === undefined) {
    // The WebAssembly runtime hasn't loaded yet, and this is the first
    // time auto mode is being requested.
    // We'll wait a bit for the WebAssembly runtime to load before making
    // a render mode decision.
    autoModeTimeoutState = 'waiting';
    setTimeout(() => {
      autoModeTimeoutState = 'timed out';

      // We want to ensure that we activate any markers whose render mode didn't get resolved
      // earlier.
      handleUpdatedComponentDescriptors();
    }, autoModeWebAssemblyTimeoutMilliseconds);
  }

  return null;
}

function registerComponentDescriptor(descriptor: ComponentDescriptor) {
  rootComponentManager.registerComponentDescriptor(descriptor);

  if (descriptor.type === 'auto') {
    startLoadingWebAssemblyIfNotStarted();
  } else if (descriptor.type === 'server') {
    startCircuitIfNotStarted();
  } else if (descriptor.type === 'webassembly') {
    startWebAssemblyIfNotStarted();
  }
}

function handleUpdatedComponentDescriptors() {
  const shouldAddNewRootComponents = !isPerformingEnhancedPageLoad();
  rootComponentManager.handleUpdatedRootComponents(shouldAddNewRootComponents);
}

async function startCircuitIfNotStarted() {
  if (hasCircuitStarted) {
    return;
  }

  hasCircuitStarted = true;
  await startCircuit(rootComponentManager);
  await waitForRendererAttached(WebRendererId.Server);
  handleUpdatedComponentDescriptors();
}

async function startLoadingWebAssemblyIfNotStarted() {
  if (webAssemblyLoadingState >= WebAssemblyLoadingState.Loading) {
    return;
  }

  webAssemblyLoadingState = WebAssemblyLoadingState.Loading;
  await loadWebAssemblyPlatform();
  webAssemblyLoadingState = WebAssemblyLoadingState.Loaded;
}

async function startWebAssemblyIfNotStarted() {
  if (webAssemblyLoadingState >= WebAssemblyLoadingState.Starting) {
    return;
  }

  webAssemblyLoadingState = WebAssemblyLoadingState.Starting;
  await startWebAssembly(rootComponentManager);
  await waitForRendererAttached(WebRendererId.WebAssembly);
  webAssemblyLoadingState = WebAssemblyLoadingState.Started;
  handleUpdatedComponentDescriptors();
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
