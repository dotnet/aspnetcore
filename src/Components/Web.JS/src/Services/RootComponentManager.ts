// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';

export interface RootComponentManager<FixedComponentDescriptorType> {
  getFixedComponentArray(): FixedComponentDescriptorType[];
  resolveRootComponent(selectorId: number, componentId: number): ComponentDescriptor;
}
