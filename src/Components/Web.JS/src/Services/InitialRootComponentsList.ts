// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';
import { RootComponentManager } from './RootComponentManager';

export class InitialRootComponentsList<ComponentDescriptorType extends ComponentDescriptor> implements RootComponentManager<ComponentDescriptorType> {
  public readonly descriptors: Set<unknown>;

  constructor(public readonly initialComponents: ComponentDescriptorType[]) {
    this.descriptors = new Set(initialComponents);
  }

  resolveRootComponent(selectorId: number, _componentId: number): ComponentDescriptor {
    return this.initialComponents[selectorId];
  }
}
