// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Currently this only deals with inserting streaming content into the DOM.
// Later this will be expanded to include:
//  - Progressive enhancement of navigation and form posting
//  - Preserving existing DOM elements in all the above
//  - The capabilities of Boot.Server.ts and Boot.WebAssembly.ts to handle insertion
//    of interactive components

import { DotNet } from '@microsoft/dotnet-js-interop';
import { hasCircuitStarted, setCircuitOptions, startCircuit } from './Boot.Server.Common';
import { hasWebAssemblyStarted, hasWebAssemblyStartedLoading, loadWebAssemblyPlatform, setWebAssemblyOptions, startWebAssembly, waitForBootConfigLoaded } from './Boot.WebAssembly.Common';
import { shouldAutoStart } from './BootCommon';
import { Blazor } from './GlobalExports';
import { WebStartOptions } from './Platform/WebStartOptions';
import { attachStreamingRenderingListener } from './Rendering/StreamingRendering';
import { attachProgressivelyEnhancedNavigationListener } from './Services/NavigationEnhancement';
import { ComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { RootComponentManager } from './Services/RootComponentManager';
import { DescriptorHandler, attachComponentDescriptorHandler, registerAllComponentDescriptors } from './Rendering/DomMerging/DomSync';
import { MonoConfig } from 'dotnet';

let started = false;
let loadWebAssemblyQuicklyPromise: Promise<boolean> | null = null;
const loadWebAssemblyQuicklyTimeoutMs = 3000;
const rootComponentManager = new RootComponentManager();

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }

  started = true;

  setCircuitOptions(options?.circuit);
  setWebAssemblyOptions(options?.webAssembly);

  const descriptorHandler: DescriptorHandler = {
    registerComponentDescriptor,
  };

  attachComponentDescriptorHandler(descriptorHandler);
  attachStreamingRenderingListener(options?.ssr, rootComponentManager);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(rootComponentManager);
  }

  registerAllComponentDescriptors(document);

  return Promise.resolve();
}

function registerComponentDescriptor(descriptor: ComponentDescriptor) {
  rootComponentManager.registerComponentDescriptor(descriptor);

  if (descriptor.type === 'auto') {
    startAutoModeRuntimeIfNotStarted();
  } else if (descriptor.type === 'server') {
    startCircuitIfNotStarted();
  } else if (descriptor.type === 'webassembly') {
    startWebAssemblyIfNotStarted();
  }
}

async function startAutoModeRuntimeIfNotStarted() {
  if (hasWebAssemblyStarted()) {
    return;
  }

  const didLoadWebAssemblyQuickly = await tryLoadWebAssemblyQuicklyIfNotStarted();
  if (didLoadWebAssemblyQuickly) {
    // We'll start the WebAssembly runtime so it starts getting used for auto components.
    await startWebAssemblyIfNotStarted();
  } else if (!hasWebAssemblyStarted()) {
    // WebAssembly could not load quickly. We notify the root component manager of this fact
    // so it starts using Blazor Server to activate auto components rather than waiting
    // for the WebAssembly runtime to start.
    rootComponentManager.notifyWebAssemblyFailedToLoadQuickly();
    await startCircuitIfNotStarted();
  }
}

function tryLoadWebAssemblyQuicklyIfNotStarted(): Promise<boolean> {
  if (loadWebAssemblyQuicklyPromise) {
    return loadWebAssemblyQuicklyPromise;
  }

  const loadPromise = (async () => {
    const loadWebAssemblyPromise = loadWebAssemblyIfNotStarted();
    const bootConfig = await waitForBootConfigLoaded();
    if (!areWebAssemblyResourcesLikelyCached(bootConfig)) {
      // Since we don't think WebAssembly resources are cached,
      // we can guess that we'll need to fetch resources over the network.
      // Therefore, we'll fall back to Blazor Server for now.
      return false;
    }
    await loadWebAssemblyPromise;
    return true;
  })();

  const timeoutPromise = new Promise<boolean>(resolve => {
    // If WebAssembly takes too long to load even though we think the resources
    // are cached, we'll fall back to Blazor Server.
    setTimeout(resolve, loadWebAssemblyQuicklyTimeoutMs, false);
  });

  loadWebAssemblyQuicklyPromise = Promise.race([loadPromise, timeoutPromise]);
  return loadWebAssemblyQuicklyPromise;
}

async function startCircuitIfNotStarted() {
  if (hasCircuitStarted()) {
    return;
  }

  await startCircuit(rootComponentManager);
}

async function loadWebAssemblyIfNotStarted() {
  if (hasWebAssemblyStartedLoading()) {
    return;
  }

  await loadWebAssemblyPlatform();

  const config = await waitForBootConfigLoaded();
  const hash = getWebAssemblyResourceHash(config);
  if (hash) {
    window.localStorage.setItem(hash.key, hash.value);
  }
}

async function startWebAssemblyIfNotStarted() {
  if (hasWebAssemblyStarted()) {
    return;
  }

  loadWebAssemblyIfNotStarted();
  await startWebAssembly(rootComponentManager);
}

function areWebAssemblyResourcesLikelyCached(loadedConfig: MonoConfig): boolean {
  if (!loadedConfig.cacheBootResources) {
    return false;
  }

  const hash = getWebAssemblyResourceHash(loadedConfig);
  if (!hash) {
    return false;
  }

  const existingHash = window.localStorage.getItem(hash.key);
  return hash.value === existingHash;
}

function getWebAssemblyResourceHash(config: MonoConfig): { key: string, value: string } | null {
  const hash = config.resources?.hash;
  const mainAssemblyName = config.mainAssemblyName;
  if (!hash || !mainAssemblyName) {
    return null;
  }

  return {
    key: `blazor-resource-hash:${mainAssemblyName}`,
    value: hash,
  };
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
