// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';
import { EventDescriptor } from './Events/EventDelegator';
import { enableJSRootComponents, JSComponentParametersByIdentifier, JSComponentIdentifiersByInitializer } from './JSRootComponents';

const interopMethodsByRenderer = new Map<number, DotNet.DotNetObject>();
const rendererAttachedListeners: ((browserRendererId: number) => void)[] = [];
const rendererByIdResolverMap: Map<number, [() => void | undefined, Promise<void> | undefined]> = new Map();

export function attachRendererIdResolver(rendererId: number, resolver: () => void | undefined, promise: Promise<void> | undefined) {
  rendererByIdResolverMap.set(rendererId, [resolver, promise]);
}

export function getRendererAttachedPromise(rendererId: number): Promise<void> | undefined {
  return rendererByIdResolverMap.get(rendererId)?.[1];
}

export function attachWebRendererInterop(
  rendererId: number,
  interopMethods: DotNet.DotNetObject,
  jsComponentParameters?: JSComponentParametersByIdentifier,
  jsComponentInitializers?: JSComponentIdentifiersByInitializer,
): void {
  if (interopMethodsByRenderer.has(rendererId)) {
    throw new Error(`Interop methods are already registered for renderer ${rendererId}`);
  }

  interopMethodsByRenderer.set(rendererId, interopMethods);

  if (jsComponentParameters && jsComponentInitializers && Object.keys(jsComponentParameters).length > 0) {
    const manager = getInteropMethods(rendererId);
    enableJSRootComponents(manager, jsComponentParameters, jsComponentInitializers);
  }

  rendererByIdResolverMap.get(rendererId)?.[0]?.();

  invokeRendererAttachedListeners(rendererId);
}

export function detachWebRendererInterop(rendererId: number): DotNet.DotNetObject {
  const interopMethods = interopMethodsByRenderer.get(rendererId);
  if (!interopMethods) {
    throw new Error(`Interop methods are not registered for renderer ${rendererId}`);
  }

  interopMethodsByRenderer.delete(rendererId);
  return interopMethods;
}

export function isRendererAttached(browserRendererId: number): boolean {
  return interopMethodsByRenderer.has(browserRendererId);
}

export function registerRendererAttachedListener(listener: (browserRendererId: number) => void) {
  rendererAttachedListeners.push(listener);
}

function invokeRendererAttachedListeners(browserRendererId: number) {
  for (const listener of rendererAttachedListeners) {
    listener(browserRendererId);
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
