// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';
import { RootComponentManager } from './RootComponentManager';

export class FixedRootComponentManager<FixedComponentDescriptorType extends ComponentDescriptor> implements RootComponentManager<FixedComponentDescriptorType> {
  constructor(private readonly _initialComponents: FixedComponentDescriptorType[]) {
  }

  getFixedComponentArray(): FixedComponentDescriptorType[] {
    return this._initialComponents;
  }

  resolveRootComponent(selectorId: number): ComponentDescriptor {
    return this._initialComponents[selectorId];
  }
}
