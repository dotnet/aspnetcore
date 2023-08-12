// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { EventDescriptor } from './Events/EventDelegator';
import { enableJSRootComponents, JSComponentParametersByIdentifier, JSComponentIdentifiersByInitializer } from './JSRootComponents';

const interopMethodsByRenderer = new Map<number, DotNet.DotNetObject>();
const resolveAttachedPromiseByRenderer = new Map<number, () => void>();
const attachedPromisesByRenderer = new Map<number, Promise<void>>();

let resolveFirstRendererAttached : () => void;

export const firstRendererAttached = new Promise<void>((resolve) => {
  resolveFirstRendererAttached = resolve;
});

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

  resolveFirstRendererAttached();
  resolveRendererAttached(rendererId);
}

export function isRendererAttached(browserRendererId: number): boolean {
  return interopMethodsByRenderer.has(browserRendererId);
}

export function waitForRendererAttached(browserRendererId: number): Promise<void> {
  if (isRendererAttached(browserRendererId)) {
    return Promise.resolve();
  }

  let attachedPromise = attachedPromisesByRenderer.get(browserRendererId);
  if (!attachedPromise) {
    attachedPromise = new Promise<void>((resolve) => {
      resolveAttachedPromiseByRenderer.set(browserRendererId, resolve);
    });
    attachedPromisesByRenderer.set(browserRendererId, attachedPromise);
  }

  return attachedPromise;
}

function resolveRendererAttached(browserRendererId: number): void {
  const resolveRendererAttached = resolveAttachedPromiseByRenderer.get(browserRendererId);
  if (resolveRendererAttached) {
    resolveAttachedPromiseByRenderer.delete(browserRendererId);
    attachedPromisesByRenderer.delete(browserRendererId);
    resolveRendererAttached();
  }
}

export function dispatchEvent(browserRendererId: number, eventDescriptor: EventDescriptor, eventArgs: any): void {
  return dispatchEventMiddleware(browserRendererId, eventDescriptor.eventHandlerId, () => {
    const interopMethods = getInteropMethods(browserRendererId);
    return interopMethods.invokeMethodAsync('DispatchEventAsync', eventDescriptor, eventArgs);
  });
}

export function updateRootComponents(browserRendererId: number, operationsJson: string): Promise<void> {
  const interopMethods = getInteropMethods(browserRendererId);
  return interopMethods.invokeMethodAsync('UpdateRootComponents', operationsJson);
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
export function addDispatchEventMiddleware(middleware: DispatchEventMiddlware): void {
  const next = dispatchEventMiddleware;
  dispatchEventMiddleware = (browserRendererId, eventHandlerId, continuation) => {
    middleware(browserRendererId, eventHandlerId, () => next(browserRendererId, eventHandlerId, continuation));
  };
}
