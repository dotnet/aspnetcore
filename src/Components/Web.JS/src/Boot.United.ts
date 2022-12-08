// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { UnitedStartOptions } from './Platform/Circuits/UnitedStartOptions';
import { startCircuit } from './Boot.Server.Common';
import { AutoComponentDescriptor, discoverComponents, ServerComponentDescriptor, WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { beginLoadingDotNetRuntime, loadBootConfig, startWebAssembly } from './Boot.WebAssembly.Common';
import { loadWebAssemblyResources } from './Platform/Mono/MonoPlatform';

let started = false;

async function boot(options?: Partial<UnitedStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;

  const autoComponents = discoverComponents(document, 'auto') as AutoComponentDescriptor[];
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  // Decide out what to do about any 'auto' components. Having any of them is a trigger to start fetching the WebAssembly
  // runtime files if they aren't already cached. If they are already cached, we'll use WebAssembly, and if not, Server.
  if (autoComponents.length) {
    // Because there is at least one 'auto' component, start loading the WebAssembly runtime
    const webAssemblyBootConfigResult = await loadBootConfig(options?.webAssembly);
    const webAssemblyResourceLoader = await beginLoadingDotNetRuntime(options?.webAssembly ?? {}, webAssemblyBootConfigResult);
    const { assembliesBeingLoaded, pdbsBeingLoaded, wasmBeingLoaded } = loadWebAssemblyResources(webAssemblyResourceLoader);
    const totalResources = assembliesBeingLoaded.concat(pdbsBeingLoaded, wasmBeingLoaded);
    let finishedCount = 0;
    totalResources.map(r => r.response.then(() => finishedCount++));
    await new Promise(r => setTimeout(r, 50)); // This is obviously not correct. Need a robust way to know how much is cached instead of just waiting a while.

    const runAutoOnServer = finishedCount < totalResources.length;
    if (runAutoOnServer) {
      serverComponents.push(...autoComponents.map(c => c.serverDescriptor));
    } else {
      webAssemblyComponents.push(...autoComponents.map(c => c.webAssemblyDescriptor));
    }
  }

  if (serverComponents.length && webAssemblyComponents.length) {
    throw new Error('TODO: Support having both Server and WebAssembly components at the same time. Not doing that currently as it overlaps with a different prototype.');
  }

  // Only set up a circuit if the page actually contains interactive server components
  // Later on, when we add progressive enhancement to navigation, we will also want to
  // auto-close circuits when the last root component is removed.
  if (serverComponents.length) {
    await startCircuit(options?.circuit, serverComponents);
  }

  if (webAssemblyComponents.length) {
    await startWebAssembly(options?.webAssembly, webAssemblyComponents);
  }
}

Blazor.start = boot;

if (shouldAutoStart()) {
  // We want to activate interactive components whenever they get added into the page, e.g., by streaming prerendering.
  // But that's nontrivial so as a quick hack, just wait until any streaming prerendering is finished before starting interactivity.
  // Ideally we'd start interactivity immediately and react whenever passive content is updated later.
  const onReadyStateChange = () => {
    if (document.readyState === 'complete') {
      document.removeEventListener('readystatechange', onReadyStateChange);
      boot();
    }
  };
  document.addEventListener('readystatechange', onReadyStateChange);
}
