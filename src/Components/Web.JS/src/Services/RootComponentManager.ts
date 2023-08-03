// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor, ComponentMarker, descriptorToMarker } from './ComponentDescriptorDiscovery';
import { isRendererAttached, updateRootComponents, waitForRendererAttached } from '../Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../Rendering/WebRendererId';
import { NavigationEnhancementCallbacks, isPerformingEnhancedPageLoad } from './NavigationEnhancement';
import { hasWebAssemblyStarted } from '../Boot.WebAssembly.Common';

type RootComponentOperation = RootComponentAddOperation | RootComponentUpdateOperation | RootComponentRemoveOperation;

type RootComponentAddOperation = {
  type: 'add';
  selectorId: number;
  marker: ComponentMarker;
};

type RootComponentUpdateOperation = {
  type: 'update';
  componentId: number;
  marker: ComponentMarker;
};

type RootComponentRemoveOperation = {
  type: 'remove';
  componentId: number;
};

type RootComponentInfo = {
  assignedRendererId?: WebRendererId;
  uniqueIdAtLastUpdate?: number;
  interactiveComponentId?: number;
}

export class RootComponentManager implements NavigationEnhancementCallbacks {
  private readonly _activeDescriptors = new Set<ComponentDescriptor>();

  private readonly _descriptorsPendingInteractivityById: { [id: number]: ComponentDescriptor } = {};

  private readonly _rootComponentInfoByDescriptor = new Map<ComponentDescriptor, RootComponentInfo>();

  private _didWebAssemblyFailToLoadQuickly = false;

  public constructor() {
    this.updateAllRootComponentsAfterRendererAttached(WebRendererId.Server);
    this.updateAllRootComponentsAfterRendererAttached(WebRendererId.WebAssembly);
  }

  public registerComponentDescriptor(descriptor: ComponentDescriptor) {
    this._activeDescriptors.add(descriptor);
  }

  public notifyWebAssemblyFailedToLoadQuickly() {
    if (!this._didWebAssemblyFailToLoadQuickly) {
      this._didWebAssemblyFailToLoadQuickly = true;
      this.updateAllRootComponents();
    }
  }

  public documentUpdated() {
    this.updateAllRootComponents();
  }

  private unregisterComponentDescriptor(descriptor: ComponentDescriptor) {
    this._activeDescriptors.delete(descriptor);
  }

  private async updateAllRootComponentsAfterRendererAttached(browserRendererId: WebRendererId) {
    await waitForRendererAttached(browserRendererId);
    this.updateAllRootComponents();
  }

  // This function should be called each time we think an SSR update
  // should be reflected in an interactive component renderer.
  // Examples include component descriptors updating, document content changing,
  // or an interactive renderer attaching for the first time.
  private updateAllRootComponents() {
    this.updateRootComponents(this._activeDescriptors);
  }

  private updateRootComponents(descriptors: Iterable<ComponentDescriptor>) {
    const operationsByRendererId = new Map<WebRendererId, RootComponentOperation[]>();

    for (const descriptor of descriptors) {
      const componentInfo = this.getRootComponentInfo(descriptor);
      const operation = this.determinePendingOperation(descriptor, componentInfo);
      if (!operation) {
        continue;
      }

      const rendererId = componentInfo.assignedRendererId;
      if (!rendererId) {
        throw new Error('Descriptors must be assigned a renderer ID before getting used as root components');
      }

      let operations = operationsByRendererId.get(rendererId);
      if (!operations) {
        operations = [];
        operationsByRendererId.set(rendererId, operations);
      }

      operations.push(operation);
    }

    for (const [rendererId, operations] of operationsByRendererId) {
      const operationsJson = JSON.stringify(operations);
      updateRootComponents(rendererId, operationsJson);
    }
  }

  private getRendererIdForDescriptor(descriptor: ComponentDescriptor): WebRendererId | null {
    const resolvedType = descriptor.type === 'auto' ? this.getAutoRenderMode() : descriptor.type;
    switch (resolvedType) {
      case 'server':
        return WebRendererId.Server;
      case 'webassembly':
        return WebRendererId.WebAssembly;
      case null:
        return null;
    }
  }

  private getAutoRenderMode(): 'webassembly' | 'server' | null {
    // If the WebAssembly runtime has started, we will always use WebAssembly
    // for auto components. Otherwise, we'll wait to activate root components
    // until we determine whether the WebAssembly runtime can be loaded quickly.

    if (hasWebAssemblyStarted()) {
      return 'webassembly';
    }

    if (this._didWebAssemblyFailToLoadQuickly) {
      return 'server';
    }

    return null;
  }

  private determinePendingOperation(descriptor: ComponentDescriptor, componentInfo: RootComponentInfo): RootComponentOperation | null {
    if (isDescriptorInDocument(descriptor)) {
      if (componentInfo.assignedRendererId === undefined) {
        // We haven't added this component for interactivity yet.
        if (isPerformingEnhancedPageLoad()) {
          // We don't add new components during enhanced page loads.
          return null;
        }

        const rendererId = this.getRendererIdForDescriptor(descriptor);
        if (rendererId === null) {
          // The renderer ID for the component has not been decided yet,
          // probably because the component has an "auto" render mode.
          return null;
        }

        if (!isRendererAttached(rendererId)) {
          // The renderer for this descriptor is not attached, so we'll no-op.
          // After the renderer attaches, we'll handle this descriptor again if
          // it's still in the document.
          return null;
        }

        componentInfo.assignedRendererId = rendererId;
        componentInfo.uniqueIdAtLastUpdate = descriptor.uniqueId;
        this._descriptorsPendingInteractivityById[descriptor.uniqueId] = descriptor;

        return { type: 'add', selectorId: descriptor.uniqueId, marker: descriptorToMarker(descriptor) };
      }

      if (componentInfo.uniqueIdAtLastUpdate === descriptor.uniqueId) {
        // The descriptor has not changed since the last update.
        // Nothing to do.
        return null;
      }

      if (componentInfo.interactiveComponentId !== undefined) {
        // The component has become interactive, so we'll update its parameters.
        componentInfo.uniqueIdAtLastUpdate = descriptor.uniqueId;
        return { type: 'update', componentId: componentInfo.interactiveComponentId, marker: descriptorToMarker(descriptor) };
      }

      // We have started to add the component, but it has not become interactive yet.
      // We'll wait until we have a component ID to work with before sending parameter
      // updates.
    } else {
      this.unregisterComponentDescriptor(descriptor);

      if (componentInfo.interactiveComponentId !== undefined) {
        // We have an interactive component for this marker, so we'll remove it.
        return { type: 'remove', componentId: componentInfo.interactiveComponentId };
      }

      // If we make it here, that means we either:
      // 1. Haven't started to make the component interactive, in which case we have no further action to take.
      // 2. Have started to make the component interactive, but it hasn't become interactive yet. In this case,
      //    we'll wait to remove the component until after we have a component ID to provide.
    }

    return null;
  }

  public resolveRootComponent(selectorId: number, componentId: number): ComponentDescriptor {
    const descriptor = this._descriptorsPendingInteractivityById[selectorId];
    if (!descriptor) {
      throw new Error(`Could not resolve a root component for descriptor with ID '${selectorId}'.`);
    }

    const rootComponentInfo = this.getRootComponentInfo(descriptor);
    if (rootComponentInfo.interactiveComponentId !== undefined) {
      throw new Error('Cannot resolve a root component for the same descriptor multiple times.');
    }

    rootComponentInfo.interactiveComponentId = componentId;

    // The descriptor may have changed since the last call to handleUpdatedRootComponentsCore().
    // We'll update this single descriptor so that the component receives the most up-to-date parameters
    // or gets removed if it no longer exists on the page.
    this.updateRootComponents([descriptor]);

    return descriptor;
  }

  private getRootComponentInfo(descriptor: ComponentDescriptor): RootComponentInfo {
    let rootComponentInfo = this._rootComponentInfoByDescriptor.get(descriptor);
    if (!rootComponentInfo) {
      rootComponentInfo = {};
      this._rootComponentInfoByDescriptor.set(descriptor, rootComponentInfo);
    }
    return rootComponentInfo;
  }
}

function isDescriptorInDocument(descriptor: ComponentDescriptor): boolean {
  return document.contains(descriptor.start);
}
