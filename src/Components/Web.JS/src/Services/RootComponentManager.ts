// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ComponentDescriptor } from './ComponentDescriptorDiscovery';
import { RootComponentInfo } from './WebRootComponentManager';

export interface RootComponentManager<InitialComponentsDescriptorType> {
  initialComponents: InitialComponentsDescriptorType[];
  onAfterRenderBatch?(browserRendererId: number): void;
  onAfterUpdateRootComponents?(batchId: number): void;
  onComponentReset?(descriptors: RootComponentInfo []): void;
  resolveRootComponent(ssrComponentId: number): ComponentDescriptor;
}
