// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor, ComponentMarker } from './ComponentDescriptorDiscovery';
import { isRendererAttached, updateRootComponents } from '../Rendering/WebRendererInteropMethods';

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

export class RootComponentManager {
  private readonly _registeredDescriptors = new Set<ComponentDescriptor>();

  private readonly _descriptorsToResolveById: { [id: number]: ComponentDescriptor } = {};

  private readonly _lastUpdatedIdByDescriptor = new Map<ComponentDescriptor, number>();

  private readonly _componentIdsByDescriptor = new Map<ComponentDescriptor, number>();

  constructor(private readonly _browserRendererId: number) { }

  public registerComponentDescriptor(descriptor: ComponentDescriptor) {
    this._registeredDescriptors.add(descriptor);
  }

  private unregisterComponentDescriptor(descriptor: ComponentDescriptor) {
    this._registeredDescriptors.delete(descriptor);
  }

  public async handleUpdatedRootComponents(addNewRootComponents: boolean) {
    await this.handleUpdatedRootComponentsCore(this._registeredDescriptors, addNewRootComponents);
  }

  private async handleUpdatedRootComponentsCore(descriptors: Iterable<ComponentDescriptor>, addNewRootComponents: boolean) {
    if (!isRendererAttached(this._browserRendererId)) {
      // There's no attached renderer to update interactive root components, so we'll no-op.
      // An alternative would be to asynchronously wait for the renderer to attach before
      // continuing, but that might happen at an inconvenient point in the future. For example,
      // 'addNewRootComponents' might have been specified as 'true', but this method could
      // continue execution at a time when the caller would have preferred it to be 'false'.
      return;
    }

    const operations: RootComponentOperation[] = [];

    for (const descriptor of descriptors) {
      if (isDescriptorInDocument(descriptor)) {
        if (!this.doesComponentNeedUpdate(descriptor)) {
          // The descriptor has not changed.
          continue;
        }

        if (!this.hasComponentEverBeenUpdated(descriptor)) {
          if (addNewRootComponents) {
            // This is the first time we're seeing this marker.
            this.markComponentAsUpdated(descriptor);
            this.markComponentAsPendingResolution(descriptor);
            operations.push({ type: 'add', selectorId: descriptor.id, marker: descriptor.toRecord() });
          }
          continue;
        }

        const componentId = this.getInteractiveComponentId(descriptor);
        if (componentId !== undefined) {
          // The component has become interactive, so we'll update its parameters.
          this.markComponentAsUpdated(descriptor);
          operations.push({ type: 'update', componentId, marker: descriptor.toRecord() });
          continue;
        }

        // We have started to add the component, but it has not become interactive yet.
        // We'll wait until we have a component ID to work with before sending parameter
        // updates.
      } else {
        this.unregisterComponentDescriptor(descriptor);

        const componentId = this.getInteractiveComponentId(descriptor);
        if (componentId !== undefined) {
          // We have an interactive component for this marker, so we'll remove it.
          operations.push({ type: 'remove', componentId });
          continue;
        }

        // If we make it here, that means we either:
        // 1. Haven't started to make the component interactive, in which case we have no further action to take.
        // 2. Have started to make the component interactive, but it hasn't become interactive yet. In this case,
        //    we'll wait to remove the component until after we have a component ID to provide.
      }
    }

    if (!operations.length) {
      return;
    }

    const operationsJson = JSON.stringify(operations);
    await updateRootComponents(this._browserRendererId, operationsJson);
  }

  public resolveRootComponent(resolutionId: number, componentId: number): ComponentDescriptor {
    const descriptor = this.resolveComponentById(resolutionId);
    if (!descriptor) {
      throw new Error(`Could not resolve a root component for descriptor with ID '${resolutionId}'.`);
    }

    if (this.getInteractiveComponentId(descriptor) !== undefined) {
      throw new Error('Cannot resolve a root component for the same descriptor multiple times.');
    }

    this.setInteractiveComponentId(descriptor, componentId);

    // The descriptor may have changed since the last call to handleUpdatedRootComponentsCore().
    // We'll update this single descriptor so that the component receives the most up-to-date parameters
    // or gets removed if it no longer exists on the page.
    this.handleUpdatedRootComponentsCore([descriptor], false);

    return descriptor;
  }

  private doesComponentNeedUpdate(descriptor: ComponentDescriptor) {
    return this._lastUpdatedIdByDescriptor.get(descriptor) !== descriptor.id;
  }

  private markComponentAsUpdated(descriptor: ComponentDescriptor) {
    this._lastUpdatedIdByDescriptor.set(descriptor, descriptor.id);
  }

  private markComponentAsPendingResolution(descriptor: ComponentDescriptor) {
    this._descriptorsToResolveById[descriptor.id] = descriptor;
  }

  private resolveComponentById(id: number): ComponentDescriptor | undefined {
    const result = this._descriptorsToResolveById[id];
    delete this._descriptorsToResolveById[id];
    return result;
  }

  private hasComponentEverBeenUpdated(descriptor: ComponentDescriptor) {
    return this._lastUpdatedIdByDescriptor.has(descriptor);
  }

  private getInteractiveComponentId(descriptor: ComponentDescriptor): number | undefined {
    return this._componentIdsByDescriptor.get(descriptor);
  }

  private setInteractiveComponentId(descriptor: ComponentDescriptor, componentId: number): void {
    this._componentIdsByDescriptor.set(descriptor, componentId);
  }
}

function isDescriptorInDocument(descriptor: ComponentDescriptor): boolean {
  return document.contains(descriptor.start);
}
