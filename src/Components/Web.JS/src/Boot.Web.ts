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
import { LogicalElement, getLogicalRootOriginalComponentId, moveLogicalRootToDocumentFragment } from './Rendering/LogicalElements';
import { removeRootComponentAsync } from './Rendering/WebRendererInteropMethods';
import { RootComponentInfo, rootComponentInfoPropname } from './Rendering/Renderer';

let started = false;
let webStartOptions: Partial<WebStartOptions> | undefined;

const pendingSSRComponentsByComponentId = new Map<number, SSRComponent>();
let pendingElementToFocus : Element | null = null;

interface SSRComponent {
  interactiveDocFrag?: DocumentFragment,
  serverDescriptor?: ServerComponentDescriptor,
  webAssemblyDescriptor?: WebAssemblyComponentDescriptor,
}

function boot(options?: Partial<WebStartOptions>) : Promise<void> {
  if (started) {
    throw new Error('Blazor has already started.');
  }
  started = true;
  webStartOptions = options;

  const navigationEnhancementCallbacks: NavigationEnhancementCallbacks = {
    beforeDocumentUpdated,
    afterDocumentUpdated,
  };

  attachStreamingRenderingListener(options?.ssr, navigationEnhancementCallbacks);

  if (!options?.ssr?.disableDomPreservation) {
    attachProgressivelyEnhancedNavigationListener(navigationEnhancementCallbacks);
  }

  return Promise.resolve();
}

function beforeDocumentUpdated(destinationRoot: Node, newContent?: Node, isNodeExcludedFromUpdate?: (node: Node) => boolean) {
  pendingSSRComponentsByComponentId.clear();
  pendingElementToFocus = document.activeElement;

  if (newContent) {
    const newServerComponents = discoverComponents(newContent, 'server') as ServerComponentDescriptor[];
    const newWebAssemblyComponents = discoverComponents(newContent, 'webassembly') as WebAssemblyComponentDescriptor[];

    for (const serverDescriptor of newServerComponents) {
      pendingSSRComponentsByComponentId.set(serverDescriptor.id, {
        serverDescriptor,
      });
    }

    for (const webAssemblyDescriptor of newWebAssemblyComponents) {
      // TODO: For now, we assume only one comment is present for a given component.
      // When we implement auto mode we'll have to either:
      // * Account for the possibility of mulitple comments being present.
      // * Come up with a new SSR component representation.
      if (pendingSSRComponentsByComponentId.has(webAssemblyDescriptor.id)) {
        throw new Error("The 'auto' render mode is not supported yet.");
      }
      pendingSSRComponentsByComponentId.set(webAssemblyDescriptor.id, {
        webAssemblyDescriptor,
      });
    }
  }

  const iterator = document.createNodeIterator(destinationRoot, NodeFilter.SHOW_COMMENT);
  while (iterator.nextNode()) {
    const node = iterator.referenceNode;
    const rootComponentInfo = node[rootComponentInfoPropname] as RootComponentInfo;
    if (!rootComponentInfo || isNodeExcludedFromUpdate?.(node)) {
      continue;
    }

    const nodeAsLogicalElement = node as unknown as LogicalElement;
    const ssrComponentId = getLogicalRootOriginalComponentId(nodeAsLogicalElement);
    if (!ssrComponentId) {
      throw new Error('Unknown SSR component ID for interactive root component.');
    }

    // Synchronously move the DOM owned by this component to a document fragment so
    // that the document can be updated without touching renderer-managed DOM.
    const docFrag = moveLogicalRootToDocumentFragment(nodeAsLogicalElement);

    const pendingSSRComponent = pendingSSRComponentsByComponentId.get(ssrComponentId);
    if (pendingSSRComponent) {
      // Mark the SSR component as needing to be replaced by an existing interactive component
      // after the document gets updated.
      pendingSSRComponent.interactiveDocFrag = docFrag;

      // Since the interactive component already exists, normalize the descriptor to a non-prerendered
      // format. We will later use the marker to move the interactive DOM back in place and supply
      // updated component parameters.
      const descriptor = pendingSSRComponent?.serverDescriptor ?? pendingSSRComponent.webAssemblyDescriptor!;
      const { start, end } = descriptor;
      if (end) {
        const range = new Range();
        range.setStartAfter(start);
        range.setEndAfter(end);
        range.deleteContents();
      }
    } else {
      // The interactive component has no corresponding descriptor, so we initiate an asynchronous disposal
      // of the component, which will eventually clean up its remaining browser-side state.
      removeRootComponentAsync(rootComponentInfo.browserRendererId, rootComponentInfo.componentId);
    }
  }
}

function afterDocumentUpdated() {
  const serverComponentsToAdd: ServerComponentDescriptor[] = [];
  const webAssemblyComponentsToAdd: WebAssemblyComponentDescriptor[] = [];

  for (const ssrComponent of pendingSSRComponentsByComponentId.values()) {
    const docFrag = ssrComponent.interactiveDocFrag;
    const marker = ssrComponent.serverDescriptor ?? ssrComponent.webAssemblyDescriptor!;
    if (docFrag) {
      // The component already had established content on the page, so we restore
      // the original content.
      const markerComment = marker.start;
      if (markerComment.ownerDocument !== document) {
        throw new Error('Component marker comment was not merged into the DOM.');
      }

      const markerParent = markerComment.parentNode!;
      markerParent.insertBefore(docFrag, markerComment);
      markerParent.removeChild(markerComment);
      // TODO: Update component values.
    } else {
      // Initialize new components.
      if (ssrComponent.serverDescriptor) {
        serverComponentsToAdd.push(ssrComponent.serverDescriptor);
      } else if (ssrComponent.webAssemblyDescriptor) {
        webAssemblyComponentsToAdd.push(ssrComponent.webAssemblyDescriptor);
      }
    }
  }

  if (serverComponentsToAdd.length) {
    addServerRootComponents(serverComponentsToAdd);
  }

  if (webAssemblyComponentsToAdd.length) {
    addWebAssemblyRootComponents(webAssemblyComponentsToAdd);
  }

  if ((pendingElementToFocus instanceof HTMLElement) && document.activeElement !== pendingElementToFocus) {
    pendingElementToFocus.focus();
  }

  pendingElementToFocus = null;
  pendingSSRComponentsByComponentId.clear();
}

let startCircuitPromise: Promise<void> | null = null;
async function addServerRootComponents(descriptors: ServerComponentDescriptor[]) {
  if (!startCircuitPromise) {
    startCircuitPromise = startCircuit(webStartOptions?.circuit, descriptors);
    await startCircuitPromise;
  } else {
    await startCircuitPromise;
    // TODO: Update component parameters.
  }
}

let startWebAssemblyPromise: Promise<void> | null = null;
async function addWebAssemblyRootComponents(descriptors: WebAssemblyComponentDescriptor[]) {
  if (!startWebAssemblyPromise) {
    startWebAssemblyPromise = startWebAssembly(webStartOptions?.webAssembly, descriptors);
    await startWebAssemblyPromise;
  } else {
    await startWebAssemblyPromise;
    // TODO: Update component parameters.
  }
}

Blazor.start = boot;
window['DotNet'] = DotNet;

if (shouldAutoStart()) {
  boot();
}
