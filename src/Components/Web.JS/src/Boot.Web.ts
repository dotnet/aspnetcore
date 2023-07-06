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
import { NavigationEnhancementCallbacks, attachProgressivelyEnhancedNavigationListener } from './Services/NavigationEnhancement';
import { WebAssemblyComponentDescriptor } from './Services/ComponentDescriptorDiscovery';
import { ServerComponentDescriptor, discoverComponents } from './Services/ComponentDescriptorDiscovery';
import { LogicalElement, moveLogicalRootToDocumentFragment } from './Rendering/LogicalElements';
import { disposeComponentAsync } from './Rendering/WebRendererInteropMethods';
import { RootComponentInfo, rootComponentInfoPropname } from './Rendering/Renderer';

let started = false;
let circuitStarted = false;
let webAssemblyStarted = false;
let webStartOptions: Partial<WebStartOptions> | undefined;

async function boot(options?: Partial<WebStartOptions>): Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  webStartOptions = options;

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    beforeDocumentUpdated,
    afterDocumentUpdated: activateInteractiveComponents,
  };

  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  await activateInteractiveComponents();
}

function beforeDocumentUpdated(isNodeExcludedFromUpdate?: (node: Node) => boolean) {
  const iterator = document.createNodeIterator(document, NodeFilter.SHOW_COMMENT);
  while (iterator.nextNode()) {
    const node = iterator.referenceNode;
    const rootComponentInfo = node[rootComponentInfoPropname] as RootComponentInfo;
    if (!rootComponentInfo || isNodeExcludedFromUpdate?.(node)) {
      continue;
    }

    // Synchronously move the DOM owned by this component to a document fragment so
    // that the document can be updated without touching renderer-managed DOM.
    // We then initiate an asynchronous disposal of the component, which will eventually
    // clean up its remaining browser-side state.
    moveLogicalRootToDocumentFragment(node as unknown as LogicalElement);
    disposeComponentAsync(rootComponentInfo.browserRendererId, rootComponentInfo.componentId);
  }
}

async function activateInteractiveComponents() {
  const serverComponents = discoverComponents(document, 'server') as ServerComponentDescriptor[];
  const webAssemblyComponents = discoverComponents(document, 'webassembly') as WebAssemblyComponentDescriptor[];

  if (serverComponents.length) {
    if (!circuitStarted) {
      circuitStarted = true;
      await startCircuit(webStartOptions?.circuit, serverComponents);
    }
  }

  if (webAssemblyComponents.length) {
    if (!webAssemblyStarted) {
      webAssemblyStarted = true;
      await startWebAssembly(webStartOptions?.webAssembly, webAssemblyComponents);
    }
  }
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
