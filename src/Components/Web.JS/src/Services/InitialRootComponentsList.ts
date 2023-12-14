// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';
import { RootComponentManager } from './RootComponentManager';

export class InitialRootComponentsList<ComponentDescriptorType extends ComponentDescriptor> implements RootComponentManager<ComponentDescriptorType> {
  constructor(public readonly initialComponents: ComponentDescriptorType[]) {
  }

  resolveRootComponent(ssrComponentId: number): ComponentDescriptor {
    return this.initialComponents[ssrComponentId];
  }
}
