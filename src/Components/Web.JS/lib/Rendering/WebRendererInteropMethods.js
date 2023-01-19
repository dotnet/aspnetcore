// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { enableJSRootComponents } from './JSRootComponents';
const interopMethodsByRenderer = new Map();
let resolveRendererAttached;
export const rendererAttached = new Promise((resolve) => {
    resolveRendererAttached = resolve;
});
export function attachWebRendererInterop(rendererId, interopMethods, jsComponentParameters, jsComponentInitializers) {
    if (interopMethodsByRenderer.has(rendererId)) {
        throw new Error(`Interop methods are already registered for renderer ${rendererId}`);
    }
    interopMethodsByRenderer.set(rendererId, interopMethods);
    if (Object.keys(jsComponentParameters).length > 0) {
        const manager = getInteropMethods(rendererId);
        enableJSRootComponents(manager, jsComponentParameters, jsComponentInitializers);
    }
    resolveRendererAttached();
}
export function dispatchEvent(browserRendererId, eventDescriptor, eventArgs) {
    return dispatchEventMiddleware(browserRendererId, eventDescriptor.eventHandlerId, () => {
        const interopMethods = getInteropMethods(browserRendererId);
        return interopMethods.invokeMethodAsync('DispatchEventAsync', eventDescriptor, eventArgs);
    });
}
function getInteropMethods(rendererId) {
    const interopMethods = interopMethodsByRenderer.get(rendererId);
    if (!interopMethods) {
        throw new Error(`No interop methods are registered for renderer ${rendererId}`);
    }
    return interopMethods;
}
let dispatchEventMiddleware = (browserRendererId, eventHandlerId, continuation) => continuation();
export function setDispatchEventMiddleware(middleware) {
    dispatchEventMiddleware = middleware;
}
//# sourceMappingURL=WebRendererInteropMethods.js.map