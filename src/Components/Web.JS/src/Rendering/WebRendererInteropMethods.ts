// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { EventDescriptor } from './Events/EventDelegator';
import { enableJSRootComponents, JSComponentParametersByIdentifier, JSComponentIdentifiersByInitializer } from './JSRootComponents';

const interopMethodsByRenderer = new Map<number, DotNet.DotNetObject>();

export function attachWebRendererInterop(
  rendererId: number,
  interopMethods: DotNet.DotNetObject,
  jsComponentParameters: JSComponentParametersByIdentifier,
  jsComponentInitializers: JSComponentIdentifiersByInitializer,
): void {
  if (interopMethodsByRenderer.has(rendererId)) {
    throw new Error(`Interop methods are already registered for renderer ${rendererId}`);
  }

  interopMethodsByRenderer.set(rendererId, interopMethods);

  if (Object.keys(jsComponentParameters).length > 0) {
    const manager = getInteropMethods(rendererId);
    enableJSRootComponents(manager, jsComponentParameters, jsComponentInitializers);
  }
}

export function dispatchEvent(browserRendererId: number, eventDescriptor: EventDescriptor, eventArgs: any) {
  return dispatchEventMiddleware(browserRendererId, eventDescriptor.eventHandlerId, () => {
    const interopMethods = getInteropMethods(browserRendererId);
    return interopMethods.invokeMethodAsync('DispatchEventAsync', eventDescriptor, eventArgs);
  });
}

function getInteropMethods(rendererId: number): DotNet.DotNetObject {
  const interopMethods = interopMethodsByRenderer.get(rendererId);
  if (!interopMethods) {
    throw new Error(`No interop methods are registered for renderer ${rendererId}`);
  }

  return interopMethods;
}

// On some hosting platforms, we may need to defer the event dispatch, so they can register this middleware to do so
type DispatchEventMiddlware = (browserRendererId: number, eventHandlerId: number, continuation: () => void) => void;
let dispatchEventMiddleware: DispatchEventMiddlware = (browserRendererId, eventHandlerId, continuation) => continuation();
export function setDispatchEventMiddleware(middleware: DispatchEventMiddlware): void {
  dispatchEventMiddleware = middleware;
}
