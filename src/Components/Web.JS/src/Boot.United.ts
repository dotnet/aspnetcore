// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Blazor } from './GlobalExports';
import { shouldAutoStart } from './BootCommon';
import { UnitedStartOptions } from './Platform/Circuits/UnitedStartOptions';
import { activatePrerenderedComponents, startCircuit } from './Boot.Server.Common';
import { AutoComponentDescriptor, discoverComponents, ServerComponentDescriptor, WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { beginLoadingDotNetRuntime, loadBootConfig, startWebAssembly } from './Boot.WebAssembly.Common';
import { loadWebAssemblyResources } from './Platform/Mono/MonoPlatform';
import { enableFormEnhancement } from './FormEnhancement';
import { enableNavigationEnhancement, performEnhancedPageLoad } from './NavigationEnhancement';
import { RootComponentsFunctions } from './Rendering/JSRootComponents';
import { WebAssemblyComponentAttacher } from './Platform/WebAssemblyComponentAttacher';
import { synchronizeDOMContent } from './DomSync/DomSync';

let booted = false;
let hasStartedWebAssembly = false;
let hasStartedServer = false;

Blazor._internal.mergePassiveContentIntoDOM = function (html: string) {
  synchronizeDOMContent({ parent: document }, html);
}

async function boot(options?: Partial<UnitedStartOptions>): Promise<void> {
  if (booted) {
    throw new Error('Blazor has already started.');
  }
  booted = true;
  await activateInteractiveComponents(options);

  enableFormEnhancement();
  enableNavigationEnhancement(() => activateInteractiveComponents(options));
}

async function activateInteractiveComponents(options?: Partial<UnitedStartOptions>) {
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

  if ((serverComponents.length && webAssemblyComponents.length)
    || (serverComponents.length && hasStartedWebAssembly)
    || (webAssemblyComponents.length && hasStartedServer)) {
    throw new Error('TODO: Support having both Server and WebAssembly components at the same time. Not doing that currently as it overlaps with a different prototype.');
  }

  // Only set up a circuit if the page actually contains interactive server components
  // Later on, when we add progressive enhancement to navigation, we will also want to
  // auto-close circuits when the last root component is removed.
  if (serverComponents.length) {
    if (!hasStartedServer) {
      hasStartedServer = true;
      await startCircuit(options?.circuit, serverComponents);
    } else {
      await activatePrerenderedComponents(serverComponents);
    }
  }

  if (webAssemblyComponents.length) {
    if (!hasStartedWebAssembly) {
      hasStartedWebAssembly = true;
      await startWebAssembly(options?.webAssembly, webAssemblyComponents);
    } else {
      Blazor._internal['currentComponentAttacher'] = new WebAssemblyComponentAttacher(webAssemblyComponents || []); // Super hacky global state
      await RootComponentsFunctions._internal_activatePrerenderedComponents();
    }
  }

  // TODO: There are new routing scenarios now there might not be any Router component in use but we still expect server-side routing to work.
  // For this prototype, assume there is no Router and that all programmatic navigations can be handled directly from here
  Blazor._internal.navigationManager.listenForNavigationEvents((uri: string, state: string | undefined, intercepted: boolean): Promise<void> => {
    performEnhancedPageLoad(uri);
    return null as any;
  }, null);
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
