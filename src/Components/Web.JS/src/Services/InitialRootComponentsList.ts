// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';
import { RootComponentManager } from './RootComponentManager';

export class InitialRootComponentsList<FixedComponentDescriptorType extends ComponentDescriptor> implements RootComponentManager<FixedComponentDescriptorType> {
  constructor(public readonly initialComponents: FixedComponentDescriptorType[]) {
  }

  resolveRootComponent(selectorId: number, _componentId: number): ComponentDescriptor {
    return this.initialComponents[selectorId];
  }
}
