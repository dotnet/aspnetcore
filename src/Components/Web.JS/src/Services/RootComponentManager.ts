// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor, ComponentMarker, descriptorToMarker } from './ComponentDescriptorDiscovery';
import { isRendererAttached, updateRootComponents } from '../Rendering/WebRendererInteropMethods';
import { WebRendererId } from '../Rendering/WebRendererId';

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

let resolveAutoMode: () => 'server' | 'webassembly' | null = () => {
  throw new Error('No auto mode resolver has been attached');
};

export function attachAutoModeResolver(resolver: () => 'server' | 'webassembly' | null) {
  resolveAutoMode = resolver;
}

export class RootComponentManager {
  private readonly _activeDescriptors = new Set<ComponentDescriptor>();

  private readonly _descriptorsPendingInteractivityById: { [id: number]: ComponentDescriptor } = {};

  private readonly _rootComponentInfoByDescriptor = new Map<ComponentDescriptor, RootComponentInfo>();

  public registerComponentDescriptor(descriptor: ComponentDescriptor) {
    this._activeDescriptors.add(descriptor);
  }

  private unregisterComponentDescriptor(descriptor: ComponentDescriptor) {
    this._activeDescriptors.delete(descriptor);
  }

  public handleUpdatedRootComponents(addNewRootComponents: boolean) {
    this.handleUpdatedRootComponentsCore(this._activeDescriptors, addNewRootComponents);
  }

  private handleUpdatedRootComponentsCore(descriptors: Iterable<ComponentDescriptor>, addNewRootComponents: boolean) {
    const operationsByRendererId = new Map<WebRendererId, RootComponentOperation[]>();

    for (const descriptor of descriptors) {
      const componentInfo = this.getRootComponentInfo(descriptor);
      const operation = this.determinePendingOperation(descriptor, componentInfo, addNewRootComponents);
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
    const resolvedType = descriptor.type === 'auto' ? resolveAutoMode() : descriptor.type;
    switch (resolvedType) {
      case 'server':
        return WebRendererId.Server;
      case 'webassembly':
        return WebRendererId.WebAssembly;
      case null:
        return null;
    }
  }

  private determinePendingOperation(descriptor: ComponentDescriptor, componentInfo: RootComponentInfo, addIfNewComponent?: boolean): RootComponentOperation | null {
    if (isDescriptorInDocument(descriptor)) {
      if (componentInfo.assignedRendererId === undefined) {
        // We haven't added this component for interactivity yet.
        if (!addIfNewComponent) {
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
          // An alternative would be to asynchronously wait for the renderer to attach before
          // continuing, but that might happen at an inconvenient point in the future. For example,
          // 'addNewRootComponents' might have been specified as 'true', but this method could
          // continue execution at a time when the caller would have preferred it to be 'false'.
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
    this.handleUpdatedRootComponentsCore([descriptor], false);

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
