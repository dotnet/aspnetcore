// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebRendererId } from './Rendering/WebRendererId';
import { capabilityLeaseRegistry } from './Services/CapabilityLeaseRegistry';

export const NavigationLock = {
  enableNavigationPrompt,
  disableNavigationPrompt,
};

function enableNavigationPrompt(id: string, rendererId?: WebRendererId) {
  capabilityLeaseRegistry.enableNavigationPrompt(id, rendererId ?? 0);
}

function disableNavigationPrompt(id: string, rendererId?: WebRendererId) {
  capabilityLeaseRegistry.disableNavigationPrompt(id, rendererId);
}
